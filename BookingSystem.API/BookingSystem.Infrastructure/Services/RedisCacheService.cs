using System.Text.Json;
using BookingSystem.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BookingSystem.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string key) where T : class
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key);
            if (cachedValue == null)
                return null;

            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key {Key}", key);
        }
    }

    public async Task<long> IncrementAsync(string key, TimeSpan? expiration = null)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var newValue = await db.StringIncrementAsync(key);
        if (newValue == 1 && expiration.HasValue)
        {
            await db.KeyExpireAsync(key, expiration.Value);
        }

        return newValue;
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            // Get the Redis server instance
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());

            // Find all keys matching the pattern
            var keys = server.Keys(pattern: pattern);

            // Get the database instance
            var database = _connectionMultiplexer.GetDatabase();

            // Delete all matching keys
            var keyArray = keys.ToArray();
            if (keyArray.Length > 0)
            {
                await database.KeyDeleteAsync(keyArray);
                _logger.LogInformation("Removed {Count} keys matching pattern {Pattern}", keyArray.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern {Pattern}", pattern);
        }
    }

    // Additional helper method for bulk operations
    public async Task RemoveByPrefixAsync(string prefix)
    {
        await RemoveByPatternAsync($"{prefix}*");
    }

    // Method to check if a key exists
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            return await database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists {Key}", key);
            return false;
        }
    }

    // Method to get remaining time to live for a key
    public async Task<TimeSpan?> GetTtlAsync(string key)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            return await database.KeyTimeToLiveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for cache key {Key}", key);
            return null;
        }
    }
}
