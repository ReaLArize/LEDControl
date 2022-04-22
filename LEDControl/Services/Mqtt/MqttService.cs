using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Server;

namespace LEDControl.Services.Mqtt;

public class MqttService
{
    private readonly IMqttServer _mqttServer;
    private readonly MqttServiceOptions _options;
    private readonly ILogger<MqttService> _logger;

    public MqttService(IOptions<MqttServiceOptions> options, ILogger<MqttService> logger)
    {
        _logger = logger;
        _options = options.Value;

        var mqttoptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(_options.Port)
            .WithApplicationMessageInterceptor(c =>
            {
                c.AcceptPublish = true;
                _logger.LogDebug("Message - ClientId {ClientId}, Topic {Topic}, Payload {Payload}",
                    c.ClientId, c.ApplicationMessage.Topic, 
                    c.ApplicationMessage?.Payload != null ? Encoding.UTF8.GetString(c.ApplicationMessage.Payload) : null );
            })
            .WithSubscriptionInterceptor(c =>
            {
                c.AcceptSubscription = true;
                _logger.LogDebug("Subscription - ClientId {ClientId}, Topic {Topic}", c.ClientId, c.TopicFilter.Topic);
            })
            .Build();

        _mqttServer = new MqttFactory().CreateMqttServer();
        _mqttServer.StartAsync(mqttoptions);
    }

    public void PublishMessage(string topic, string message)
    {
        _mqttServer.PublishAsync(topic, message).Wait();
    }
    
    
}