using System.Text.Json.Serialization;
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

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(GetSchemaId);
    options.SchemaFilter<EnumSchemaFilter>();
});

static string GetSchemaId(Type type)
{
    if (!type.IsGenericType)
    {
        return type.FullName?.Replace("+", ".") ?? type.Name;
    }

    var genericTypeName = type.GetGenericTypeDefinition().Name;
    var cleanGenericName = genericTypeName[..genericTypeName.IndexOf('`')];
    var genericArguments = type.GetGenericArguments().Select(GetSchemaId);

    return $"{cleanGenericName}Of{string.Join("And", genericArguments)}";
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:7232", "http://localhost:5236")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

var app = builder.Build();

app.UseCustomExceptionHandler();

app.UseRouting();
app.UseCors("AllowFrontend");

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

    // REMARK: Uncomment in the initial setup
    // if you want to seed initial data.
    // (It's not a must)
    //app.SeedData();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

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

public partial class Program;