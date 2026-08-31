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

    // Discovered (not configured) — see DiscoverKeycloakClientSecretsAsync.
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

        // Must happen before the first access to Services/Server below —
        // that access is what triggers ConfigureWebHost to actually run, and
        // it reads _adminClientSecret/_authClientSecret, which need to be
        // populated by then.
        await DiscoverKeycloakClientSecretsAsync();

        // Force Server to build now so migrations run before Respawn snapshots
        // the schema (Program.cs only calls ApplyMigrations() in Development).
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();

            // Seed reference/lookup data that migrations don't create.
            // User.Create() always inserts a user_roles row for Role.Guest,
            // so any User seeded by a test fails its FK constraint against an
            // empty roles table unless this exists first.
            // ASSUMPTION (flagged): table/column names for the Role entity's
            // EF mapping — I don't have RoleConfiguration.cs, so this assumes
            // the same snake_case convention as every other entity here
            // ("roles" table, "id"/"name" columns). Adjust if that's wrong.
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
            // "roles" excluded alongside migrations history: it's reference
            // data seeded once above, not per-test state. Respawn would
            // otherwise TRUNCATE it on the very first test's reset and break
            // every User.Create() call in every test after that.
            TablesToIgnore = ["__ef_migrations_history", "roles"]
        });

        // Separate admin connection for FLUSHDB between tests — several
        // Apartment queries (SearchApartmentsQuery, GetApartmentsByOwnerQuery)
        // implement ICachedQuery and are cached in this same Redis instance
        // via a real QueryCachingBehavior, so this needs resetting exactly
        // like Postgres does. AllowAdmin must be set explicitly — FLUSHDB is
        // rejected otherwise. This is a separate connection from the app's
        // own Redis cache connection, so admin mode is scoped to test-reset
        // use only, not the production wiring.
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

            // Scoped so the same instance a test arms via SaveChangesInterceptor
            // is the one actually used by the DbContext resolved in that scope.
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

            // Email — real SMTP is a true external boundary, so this stays mocked.
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, TestEmailService>();

            // Direct-call background scheduling (outside the Outbox path) stays
            // mocked/recording; Outbox-driven flows go through the real
            // HangfireBackgroundJobScheduler registered by AddInfrastructure.
            services.RemoveAll<IBackgroundJobScheduler>();
            services.AddScoped<IBackgroundJobScheduler, TestBackgroundJobScheduler>();

            // Payment gateway — true external boundary (Stripe).
            services.RemoveAll<IPaymentGatewayService>();
            services.AddScoped<IPaymentGatewayService, TestPaymentGatewayService>();

            // File storage — true external boundary (S3-compatible object storage).
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

            // Prevent Hangfire's own server from ever dequeuing/executing jobs
            // in the test host. Storage, job classes, and the scheduler stay
            // real and DI-resolvable; ProcessOutboxMessagesJobSetup.Start /
            // CompleteExpiredBookingsJobSetup.Start still enqueue real rows
            // into Postgres, they just sit there unconsumed. Tests trigger
            // ProcessOutboxMessagesJob.ProcessAsync explicitly instead.
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

    /// <summary>
    /// Authenticates against Keycloak's master realm as the container's
    /// bootstrap admin, then reads each client's ACTUAL secret via the Admin
    /// REST API — the same call the "regenerate secret" button in the admin
    /// console makes under the hood — rather than trusting any secret value
    /// baked into the realm export file, which Keycloak's Admin Console
    /// masks as the literal string "**********" on export. This makes the
    /// real secret discoverable at test-run time instead of needing to be
    /// kept in sync by hand between the JSON file and app config.
    ///
    /// ASSUMPTION (flagged): "admin"/"admin" is Testcontainers.Keycloak's
    /// documented default bootstrap admin for the master realm. If this
    /// container image/module version differs, override via
    /// KeycloakBuilder.WithUsername/WithPassword and adjust here.
    /// </summary>
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