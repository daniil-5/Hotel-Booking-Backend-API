using BookingSystem.Application.Interfaces;
using StackExchange.Redis;

namespace BookingSystem.Infrastructure.Messaging;

public class CacheInvalidationPublisher : ICacheInvalidationPublisher
{
    private readonly IConnectionMultiplexer _connection;

    public CacheInvalidationPublisher(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync(string entityType, string entityId)
    {
        var sub = _connection.GetSubscriber();
        await sub.PublishAsync(RedisChannel.Literal("cache:invalidate"), $"{entityType}:{entityId}");
    }
}
