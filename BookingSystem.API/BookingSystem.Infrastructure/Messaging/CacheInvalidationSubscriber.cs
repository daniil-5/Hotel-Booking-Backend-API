using BookingSystem.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BookingSystem.Infrastructure.Messaging;

public class CacheInvalidationSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _connection;
    private readonly ILogger<CacheInvalidationSubscriber> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CacheInvalidationSubscriber(
        IConnectionMultiplexer connection,
        ILogger<CacheInvalidationSubscriber> logger,
        IServiceScopeFactory scopeFactory)
    {
        _connection = connection;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sub = _connection.GetSubscriber();
        await sub.SubscribeAsync(RedisChannel.Literal("cache:invalidate"), async (channel, message) =>
        {
            var parts = message.ToString().Split(':');
            var entityType = parts[0];
            var entityId = parts[1];

            var cacheKey = $"{entityType}:id:{entityId}";

            using var scope = _scopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            await cacheService.RemoveAsync(cacheKey);

            _logger.LogInformation("[{Instance}] Cache invalidated: {Key}",
                Environment.MachineName, cacheKey);
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
