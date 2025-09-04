using StackExchange.Redis;
using System.Text.Json;

namespace BE_OPENSKY.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, expiration);
            _logger.LogDebug("Set Redis key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Redis key: {Key}", key);
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                _logger.LogDebug("Redis key not found: {Key}", key);
                return default(T);
            }

            var result = JsonSerializer.Deserialize<T>(value!);
            _logger.LogDebug("Retrieved Redis key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis key: {Key}", key);
            return default(T);
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            var result = await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Deleted Redis key: {Key}, Success: {Success}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Redis key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var result = await _database.KeyExistsAsync(key);
            _logger.LogDebug("Checked Redis key existence: {Key}, Exists: {Exists}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Redis key existence: {Key}", key);
            return false;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        try
        {
            await _database.StringSetAsync(key, value, expiration);
            _logger.LogDebug("Set Redis string key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Redis string key: {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                _logger.LogDebug("Redis string key not found: {Key}", key);
                return null;
            }

            _logger.LogDebug("Retrieved Redis string key: {Key}", key);
            return value!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis string key: {Key}", key);
            return null;
        }
    }
}
