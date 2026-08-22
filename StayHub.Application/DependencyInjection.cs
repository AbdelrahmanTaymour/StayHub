using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StayHub.Application.Abstractions.Behaviors;
using StayHub.Domain.Bookings;

namespace StayHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MediatR:LicenseKey"];

            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient<PricingService>();

        return services;
    }
}