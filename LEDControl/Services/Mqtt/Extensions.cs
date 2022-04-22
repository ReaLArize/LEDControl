using System;
using Microsoft.Extensions.DependencyInjection;

namespace LEDControl.Services.Mqtt;

public static class Extensions
{
    public static IServiceCollection AddMqttService(this IServiceCollection services,
        Action<MqttServiceOptions> options)
    {
        services.Configure(options);
        services.AddSingleton<MqttService>();
        return services;
    }
}