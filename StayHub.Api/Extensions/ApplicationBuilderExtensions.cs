using Hangfire;
using Hangfire.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StayHub.Api.Middleware;
using StayHub.Infrastructure;
using StayHub.Infrastructure.BackgroundJobs;
using StayHub.Infrastructure.Outbox;

namespace StayHub.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.Migrate();
    }

    public static void UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextLoggingMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseBackgroundProcessing(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value;

        if (!options.Enabled) return app;

        ProcessOutboxMessagesJobSetup.Start(app);
        CompleteExpiredBookingsJobSetup.Start(app);

        var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DashboardTitle = "StayHub Background Jobs",
            Authorization = environment.IsDevelopment()
                ? Array.Empty<IDashboardAuthorizationFilter>()
                : new IDashboardAuthorizationFilter[]
                {
                    new HangfireDashboardAuthorizationFilter()
                }
        });

        return app;
    }
}