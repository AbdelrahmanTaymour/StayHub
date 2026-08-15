using Asp.Versioning;
using Asp.Versioning.Builder;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using StayHub.Api.Endpoints.Apartments;
using StayHub.Api.Endpoints.Bookings;
using StayHub.Api.Endpoints.Conversations;
using StayHub.Api.Endpoints.Favorites;
using StayHub.Api.Endpoints.Maintenance;
using StayHub.Api.Endpoints.Notifications;
using StayHub.Api.Endpoints.Payments;
using StayHub.Api.Endpoints.Reviews;
using StayHub.Api.Endpoints.Users;
using StayHub.Api.Extensions;
using StayHub.Api.OpenApi;
using StayHub.Application;
using StayHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

var app = builder.Build();

app.UseCustomExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.DescribeApiVersions();

        foreach (var description in descriptions)
        {
            var url = $"/swagger/{description.GroupName}/swagger.json";
            var name = description.GroupName.ToUpperInvariant();

            options.SwaggerEndpoint(url, name);
        }
    });

    app.ApplyMigrations();
}

app.UseHttpsRedirection();

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

app.UseBackgroundProcessing();

app.MapControllers();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

var routeGroupBuilder = app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(apiVersionSet);

routeGroupBuilder.MapBookingEndpoints();
routeGroupBuilder.MapApartmentEndpoints();
routeGroupBuilder.MapPaymentEndpoints();
routeGroupBuilder.MapReviewEndpoints();
routeGroupBuilder.MapConversationEndpoints();
routeGroupBuilder.MapFavoriteEndpoints();
routeGroupBuilder.MapNotificationEndpoints();
routeGroupBuilder.MapMaintenanceEndpoints();
routeGroupBuilder.MapUserEndpoints();


app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();