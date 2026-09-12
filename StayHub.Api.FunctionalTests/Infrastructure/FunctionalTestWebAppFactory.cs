using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using StackExchange.Redis;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Email;
using StayHub.Application.Abstractions.Payments;
using StayHub.Application.Abstractions.Storage;
using StayHub.Infrastructure;
using StayHub.Infrastructure.Authentication;
using StayHub.Infrastructure.Data;
using Testcontainers.Keycloak;
using Testcontainers.Mailpit;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace StayHub.Api.FunctionalTests.Infrastructure;

public class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly KeycloakContainer _keycloakContainer;
    private readonly MailpitContainer _mailpitContainer;
    private readonly INetwork _network;
    private readonly RedisContainer _redisContainer;
    private string _adminClientSecret = string.Empty;
    private string _authClientSecret = string.Empty;
    private IConnectionMultiplexer _redisMultiplexer = null!;
    private NpgsqlConnection _respawnConnection = null!;

    private Respawner _respawner = null!;

    public FunctionalTestWebAppFactory()
    {
        _network = new NetworkBuilder().Build();

        _dbContainer = new PostgreSqlBuilder("postgres:latest")
            .WithDatabase("StayHub")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _redisContainer = new RedisBuilder("redis:latest")
            .Build();

        _mailpitContainer = new MailpitBuilder("axllent/mailpit:latest")
            .WithNetwork(_network)
            .WithNetworkAliases("mailpit")
            .Build();

        _keycloakContainer = new KeycloakBuilder("quay.io/keycloak/keycloak:26.7")
            .WithNetwork(_network)
            .WithResourceMapping(
                new FileInfo(Path.Combine(AppContext.BaseDirectory, ".files", "stayhub-realm-export.json")),
                new FileInfo("/opt/keycloak/data/import/realm.json"))
            .WithCommand("--import-realm")
            .Build();
    }

    /// <summary>Base address for querying Mailpit's REST API (GET /api/v2/search, /api/v2/message/{id}).</summary>
    public Uri MailpitApiBaseAddress => new(_mailpitContainer.GetWebAddress());

    public async Task InitializeAsync()
    {
        // 1. Create network first
        await _network.CreateAsync();

        // 2. Start all containers in parallel
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _mailpitContainer.StartAsync(),
            _keycloakContainer.StartAsync());

        // 3. Discover Keycloak secrets after container startup
        await DiscoverKeycloakClientSecretsAsync();

        // 4. Run database migrations
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        // 5. Initialize Respawner
        _respawnConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _respawnConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__ef_migrations_history", "roles", "permissions", "role_permissions"]
        });

        // 6. Connect Redis Multiplexer
        var redisConfiguration = ConfigurationOptions.Parse(_redisContainer.GetConnectionString());
        redisConfiguration.AllowAdmin = true;

        _redisMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConfiguration);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        await _redisMultiplexer.DisposeAsync();
        await _respawnConnection.DisposeAsync();

        await Task.WhenAll(
            _dbContainer.StopAsync(),
            _redisContainer.StopAsync(),
            _mailpitContainer.StopAsync(),
            _keycloakContainer.StopAsync());

        await _network.DeleteAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("BackgroundJobs:Enabled", "true");
        builder.UseSetting("Keycloak:BaseUrl", _keycloakContainer.GetBaseAddress().TrimEnd('/'));
        builder.UseSetting("ConnectionStrings:Database", _dbContainer.GetConnectionString());
        builder.UseSetting("Keycloak:AdminClientSecret", _adminClientSecret);
        builder.UseSetting("Keycloak:AuthClientSecret", _authClientSecret);
        builder.UseSetting("Stripe:WebhookSecret", "whsec_test_secret_for_functional_tests");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseNpgsql(_dbContainer.GetConnectionString())
                    .UseSnakeCaseNamingConvention());

            services.RemoveAll(typeof(ISqlConnectionFactory));

            services.AddSingleton<ISqlConnectionFactory>(_ =>
                new SqlConnectionFactory(_dbContainer.GetConnectionString()));

            services.Configure<RedisCacheOptions>(options =>
                options.Configuration = _redisContainer.GetConnectionString());

            var keycloakAddress = _keycloakContainer.GetBaseAddress().TrimEnd('/');

            services.Configure<KeycloakOptions>(options =>
            {
                options.AdminUrl = $"{keycloakAddress}/admin/realms/StayHub/";
                options.TokenUrl = $"{keycloakAddress}/realms/StayHub/protocol/openid-connect/token";
            });

            services.Configure<AuthenticationOptions>(options =>
            {
                options.Issuer = $"{keycloakAddress}/realms/StayHub";
                options.MetadataUrl = $"{keycloakAddress}/realms/StayHub/.well-known/openid-configuration";
            });

            // Only true external third-party boundaries get swapped.
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, TestEmailService>();

            services.RemoveAll<IPaymentGatewayService>();
            services.AddSingleton<IPaymentGatewayService, TestPaymentGatewayService>();

            services.RemoveAll<IFileStorageService>();
            services.AddScoped<IFileStorageService, TestFileStorageService>();
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_respawnConnection);
    }

    public async Task ResetCacheAsync()
    {
        foreach (var endpoint in _redisMultiplexer.GetEndPoints())
        {
            await _redisMultiplexer.GetServer(endpoint).FlushDatabaseAsync();
        }
    }

    /// <summary>Clears Mailpit's inbox. Call per-test (e.g. in BaseFunctionalTest.InitializeAsync)
    /// so one test's reset email doesn't leak into another's assertions.</summary>
    public async Task ResetMailpitAsync()
    {
        using var client = new HttpClient { BaseAddress = MailpitApiBaseAddress };
        await client.DeleteAsync("api/v1/messages");
    }

    public async Task PromoteToAdminAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO user_roles (user_id, role_id) VALUES ({userId}, 2) ON CONFLICT DO NOTHING;");
    }

    private async Task DiscoverKeycloakClientSecretsAsync()
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_keycloakContainer.GetBaseAddress());

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