namespace BookingSystem.Application.Interfaces;

public interface IKafkaProducer
{
    Task SendMessageAsync<T>(string topic, string key, T message);
}
