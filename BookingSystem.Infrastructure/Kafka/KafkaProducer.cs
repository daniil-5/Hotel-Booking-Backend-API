using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Settings;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BookingSystem.Infrastructure.Kafka;

public class KafkaProducer : IKafkaProducer
{
    private readonly ProducerConfig _config;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaSettings> kafkaSettings, ILogger<KafkaProducer> logger)
    {
        _config = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.Value.BootstrapServers
        };
        _logger = logger;
    }

    public async Task SendMessageAsync<T>(string topic, string key, T message)
    {
        _logger.LogInformation("Preparing to send Kafka message. Topic: {Topic}, Key: {Key}", topic, key);

        try
        {
            using var producer = new ProducerBuilder<string, string>(_config).Build();
            var json = JsonSerializer.Serialize(message);

            var deliveryResult = await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = json
            });

            _logger.LogInformation("Message delivered successfully to {Topic} [[{Partition}]] @ {Offset}",
                deliveryResult.Topic,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka delivery failed for Topic: {Topic}. Reason: {Reason}", topic, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error producing message to Topic: {Topic}", topic);
            throw;
        }
    }
}