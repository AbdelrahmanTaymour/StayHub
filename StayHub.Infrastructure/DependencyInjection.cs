using Amazon.S3;
using Asp.Versioning;
using Dapper;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Email;
using StayHub.Application.Abstractions.Payments;
using StayHub.Application.Abstractions.Storage;
using StayHub.Application.Bookings;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Auditing;
using StayHub.Domain.Bookings;
using StayHub.Domain.Conversations;
using StayHub.Domain.Favorites;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Notifications;
using StayHub.Domain.Payments;
using StayHub.Domain.Reviews;
using StayHub.Domain.Users;
using StayHub.Infrastructure.Authentication;
using StayHub.Infrastructure.Authorization;
using StayHub.Infrastructure.BackgroundJobs;
using StayHub.Infrastructure.Caching;
using StayHub.Infrastructure.Clock;
using StayHub.Infrastructure.Data;
using StayHub.Infrastructure.Email;
using StayHub.Infrastructure.Outbox;
using StayHub.Infrastructure.Payments;
using StayHub.Infrastructure.Repositories;
using StayHub.Infrastructure.Storage;
using AuthenticationOptions = StayHub.Infrastructure.Authentication.AuthenticationOptions;
using AuthenticationService = StayHub.Infrastructure.Authentication.AuthenticationService;
using IAuthenticationService = StayHub.Application.Abstractions.Authentication.IAuthenticationService;

namespace StayHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        var connectionString =
            configuration.GetConnectionString("Database") ??
            throw new InvalidOperationException("Connection string 'Database' was not found.");

        AddPersistence(services, connectionString);
        AddEmail(services, configuration);
        AddBackgroundJobs(services, configuration, connectionString);
        AddRepositories(services);
        AddPayments(services, configuration);
        AddFileStorage(services, configuration);
        AddAuthentication(services, configuration);
        AddAuthorization(services);
        AddCaching(services, configuration);
        AddHealthChecks(services, configuration);

        AddApiVersioning(services);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new AmenityListTypeHandler());
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();

        services.AddScoped<IApartmentRepository, ApartmentRepository>();
        services.AddScoped<IApartmentImageRepository, ApartmentImageRepository>();
        services.AddScoped<IApartmentAvailabilityBlockRepository, ApartmentAvailabilityBlockRepository>();
        services.AddScoped<IApartmentStaffAssignmentRepository, ApartmentStaffAssignmentRepository>();
        services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewResponseRepository, ReviewResponseRepository>();
        services.AddScoped<IFavoriteApartmentRepository, FavoriteApartmentRepository>();

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
    }

    private static void AddPayments(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));

        services.AddScoped<IPaymentGatewayService, StripePaymentGatewayService>();
        services.AddScoped<StripeWebhookEventParser>();
    }

    private static void AddFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));

        services.AddSingleton<IAmazonS3>(sp =>
            ObjectStorageClientFactory.Create(sp.GetRequiredService<IOptions<StorageSettings>>()));

        services.AddSingleton<IImageProcessor, ImageProcessor>();
        services.AddScoped<IFileStorageService, ObjectStorageService>();
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));

        services.ConfigureOptions<JwtBearerOptionsSetup>();

        services.Configure<KeycloakOptions>(configuration.GetSection("Keycloak"));

        services.AddTransient<AdminAuthorizationDelegatingHandler>();

        services.AddHttpClient<IAuthenticationService, AuthenticationService>((serviceProvider, httpClient) =>
            {
                var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

                httpClient.BaseAddress = new Uri(keycloakOptions.AdminUrl);
            })
            .AddHttpMessageHandler<AdminAuthorizationDelegatingHandler>();

        services.AddHttpClient<IJwtService, JwtService>((serviceProvider, httpClient) =>
        {
            var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

            httpClient.BaseAddress = new Uri(keycloakOptions.TokenUrl);
        });

        services.AddHttpContextAccessor();

        services.AddScoped<IUserContext, UserContext>();
    }

    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddScoped<AuthorizationService>();

        services.AddTransient<IClaimsTransformation, CustomClaimsTransformation>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Cache") ??
            throw new ArgumentNullException(nameof(configuration));

        services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);

        services.AddSingleton<ICacheService, CacheService>();
    }

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!)
            .AddRedis(configuration.GetConnectionString("Cache")!)
            .AddUrlGroup(
                new Uri(configuration["Keycloak:BaseUrl"]!),
                HttpMethod.Get, "keyclock");
    }

    private static void AddApiVersioning(IServiceCollection service)
    {
        service
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddSingleton<SmtpEmailSender>();

        services.AddScoped<SendEmailJob>();

        services.AddScoped<IEmailService, QueuedEmailService>();
    }

    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration,
        string connectionString)
    {
        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));

        services.Configure<CompleteExpiredBookingsJobOptions>(
            configuration.GetSection(CompleteExpiredBookingsJobOptions.SectionName));

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        services.AddScoped<ProcessOutboxMessagesJob>();
        services.AddScoped<CompleteExpiredBookingsJob>();
    }
}