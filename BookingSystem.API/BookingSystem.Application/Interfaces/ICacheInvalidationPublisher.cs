namespace BookingSystem.Application.Interfaces;

public interface ICacheInvalidationPublisher
{
    Task PublishAsync(string entityType, string entityId);
}
