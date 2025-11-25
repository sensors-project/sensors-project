using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using backend.Models;

namespace backend.Services;

public class MqttService : IHostedService
{
    private readonly ILogger<MqttService> _logger;
    private readonly MongoDbService _mongoDbService;
    private readonly IConfiguration _configuration;
    private IManagedMqttClient? _mqttClient;

    public MqttService(
        ILogger<MqttService> logger, 
        MongoDbService mongoDbService,
        IConfiguration configuration)
    {
        _logger = logger;
        _mongoDbService = mongoDbService;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var broker = _configuration["MQTT_BROKER"] 
            ?? Environment.GetEnvironmentVariable("MQTT_BROKER") 
            ?? "localhost";
        
        var portStr = _configuration["MQTT_PORT"] 
            ?? Environment.GetEnvironmentVariable("MQTT_PORT") 
            ?? "1883";
        
        var port = int.Parse(portStr);

        _logger.LogInformation("Starting MQTT service, connecting to {Broker}:{Port}", broker, port);

        var mqttFactory = new MqttFactory();
        _mqttClient = mqttFactory.CreateManagedMqttClient();

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithClientId($"backend-{Guid.NewGuid()}")
                .Build())
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

        await _mqttClient.StartAsync(options);

        // Subscribe to all sensor topics
        var topics = new[]
        {
            "sensors/temperature",
            "sensors/pressure",
            "sensors/co2",
            "sensors/oxygen",
            "sensors/data"
        };

        foreach (var topic in topics)
        {
            await _mqttClient.SubscribeAsync(topic);
            _logger.LogInformation("Subscribed to topic: {Topic}", topic);
        }

        _logger.LogInformation("MQTT service started successfully");
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            
            _logger.LogDebug("Received message on topic {Topic}: {Payload}", topic, payload);

            var sensorMessage = JsonSerializer.Deserialize<SensorMessage>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (sensorMessage != null)
            {
                var sensorData = new SensorData
                {
                    SensorId = sensorMessage.SensorId,
                    SensorType = sensorMessage.SensorType,
                    Value = sensorMessage.Value,
                    Timestamp = DateTime.TryParse(sensorMessage.Timestamp, out var ts) ? ts : DateTime.UtcNow,
                    Unit = sensorMessage.Unit ?? ""
                };

                await _mongoDbService.InsertAsync(sensorData);
                _logger.LogDebug("Stored sensor data: {SensorType} - {Value} {Unit}", 
                    sensorData.SensorType, sensorData.Value, sensorData.Unit);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient != null)
        {
            await _mqttClient.StopAsync();
            _mqttClient.Dispose();
        }
        
        _logger.LogInformation("MQTT service stopped");
    }

    private class SensorMessage
    {
        public int SensorId { get; set; }
        public string SensorType { get; set; } = string.Empty;
        public double Value { get; set; }
        public string? Timestamp { get; set; }
        public string? Unit { get; set; }
    }
}
