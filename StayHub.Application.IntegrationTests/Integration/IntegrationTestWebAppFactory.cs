using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using StackExchange.Redis;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.BackgroundJobs;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Email;
using StayHub.Application.Abstractions.Payments;
using StayHub.Application.Abstractions.Storage;
using StayHub.Infrastructure;
using StayHub.Infrastructure.Authentication;
using StayHub.Infrastructure.Data;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Role = StayHub.Domain.Users.Role;

namespace StayHub.Application.IntegrationTests.Integration;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:latest")
        .WithDatabase("StayHub")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly KeycloakContainer _keycloakContainer = new KeycloakBuilder("quay.io/keycloak/keycloak:26.7")
        .WithResourceMapping(
            new FileInfo(Path.Combine(AppContext.BaseDirectory, ".files", "stayhub-realm-export.json")),
            new FileInfo("/opt/keycloak/data/import/realm.json"))
        .WithCommand("--import-realm")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:latest")
        .Build();

    private string _adminClientSecret = string.Empty;
    private string _authClientSecret = string.Empty;
    private IConnectionMultiplexer _redisMultiplexer = null!;
    private NpgsqlConnection _respawnConnection = null!;

    private Respawner _respawner = null!;

    public string KeycloakBaseAddress => _keycloakContainer.GetBaseAddress();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _keycloakContainer.StartAsync());

        await DiscoverKeycloakClientSecretsAsync();

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();

            await dbContext.Database.ExecuteSqlRawAsync("""
                                                        INSERT INTO roles (id, name)
                                                        VALUES (1, 'Guest'), (2, 'Admin')
                                                        ON CONFLICT (id) DO NOTHING;
                                                        """);
        }

        _respawnConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _respawnConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__ef_migrations_history", "roles"]
        });


        var redisConfiguration = ConfigurationOptions.Parse(_redisContainer.GetConnectionString());
        redisConfiguration.AllowAdmin = true;

        _redisMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConfiguration);
    }

    public new async Task DisposeAsync()
    {
        await _redisMultiplexer.DisposeAsync();
        await _respawnConnection.DisposeAsync();

        await _dbContainer.StopAsync();
        await _redisContainer.StopAsync();
        await _keycloakContainer.StopAsync();

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("BackgroundJobs:Enabled", "true");
        builder.UseSetting("Keycloak:BaseUrl", _keycloakContainer.GetBaseAddress());
        builder.UseSetting("ConnectionStrings:Database", _dbContainer.GetConnectionString());
        builder.UseSetting("Keycloak:AdminClientSecret", _adminClientSecret);
        builder.UseSetting("Keycloak:AuthClientSecret", _authClientSecret);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

            services.AddScoped<TestFailingSaveChangesInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
                options
                    .UseNpgsql(_dbContainer.GetConnectionString())
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(sp.GetRequiredService<TestFailingSaveChangesInterceptor>()));

            services.RemoveAll(typeof(ISqlConnectionFactory));

            services.AddSingleton<ISqlConnectionFactory>(_ =>
                new SqlConnectionFactory(_dbContainer.GetConnectionString()));

            services.Configure<RedisCacheOptions>(options =>
                options.Configuration = _redisContainer.GetConnectionString());

            services.Configure<KeycloakOptions>(options =>
            {
                var keycloakAddress = _keycloakContainer.GetBaseAddress().TrimEnd('/');
                options.AdminUrl = $"{keycloakAddress}/admin/realms/StayHub/";
                options.TokenUrl = $"{keycloakAddress}/realms/StayHub/protocol/openid-connect/token";
            });

            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, TestEmailService>();

            services.RemoveAll<IBackgroundJobScheduler>();
            services.AddScoped<IBackgroundJobScheduler, TestBackgroundJobScheduler>();

            services.RemoveAll<IPaymentGatewayService>();
            services.AddScoped<IPaymentGatewayService, TestPaymentGatewayService>();

            services.RemoveAll<IFileStorageService>();
            services.AddScoped<IFileStorageService, TestFileStorageService>();

            services.RemoveAll<IUserContext>();

            services.AddScoped<TestUserContext>(_ => new TestUserContext
            {
                UserId = Guid.CreateVersion7(),
                IdentityId = Guid.CreateVersion7().ToString(),
                Roles = [Role.Admin.Name]
            });

            services.AddScoped<IUserContext>(sp => sp.GetRequiredService<TestUserContext>());

            var hangfireHostedServiceDescriptors = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType?.Namespace?.StartsWith("Hangfire", StringComparison.Ordinal) == true)
                .ToList();

            foreach (var descriptor in hangfireHostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }
        });
    }

    public async Task ResetCacheAsync()
    {
        foreach (var endpoint in _redisMultiplexer.GetEndPoints())
        {
            await _redisMultiplexer.GetServer(endpoint).FlushDatabaseAsync();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_respawnConnection);
    }


    private async Task DiscoverKeycloakClientSecretsAsync()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(_keycloakContainer.GetBaseAddress()) };

        var tokenResponse = await httpClient.PostAsync(
            "realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = "admin",
                ["password"] = "admin"
            }));

        tokenResponse.EnsureSuccessStatusCode();

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenPayload.GetProperty("access_token").GetString();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        _adminClientSecret = await GetClientSecretAsync(httpClient, "stayhub-admin-client");
        _authClientSecret = await GetClientSecretAsync(httpClient, "stayhub-auth-client");
    }

    private static async Task<string> GetClientSecretAsync(HttpClient httpClient, string clientId)
    {
        var clients = await httpClient.GetFromJsonAsync<JsonElement[]>(
            $"admin/realms/StayHub/clients?clientId={clientId}");

        var internalId = clients![0].GetProperty("id").GetString();

        var secretPayload = await httpClient.GetFromJsonAsync<JsonElement>(
            $"admin/realms/StayHub/clients/{internalId}/client-secret");

        return secretPayload.GetProperty("value").GetString()!;
    }
}