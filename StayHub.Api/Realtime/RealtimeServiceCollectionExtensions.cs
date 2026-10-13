using System.Text.Json.Serialization;
using StayHub.Application.Abstractions.Realtime;

namespace StayHub.Api.Realtime;

public static class RealtimeServiceCollectionExtensions
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services
            .AddSignalR()
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddScoped<IRealtimeConversationNotifier, SignalRConversationNotifier>();

        return services;
    }
}