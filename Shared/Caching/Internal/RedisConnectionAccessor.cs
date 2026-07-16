using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Usm.Shared.Caching.Options;

namespace Usm.Shared.Caching.Internal;

internal sealed class RedisConnectionAccessor(
    IOptions<RedisCachingOptions> options,
    ILogger<RedisConnectionAccessor> logger) : IRedisConnectionAccessor
{
    private readonly RedisCachingOptions _options = options.Value;
    private readonly ILogger<RedisConnectionAccessor> _logger = logger;
    private Task<IConnectionMultiplexer?>? _connectionTask;
    private readonly Lock _lock = new();

    public Task<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connectionTask is not null)
            return _connectionTask;

        lock (_lock)
        {
            _connectionTask ??= ConnectAsync(cancellationToken);
            return _connectionTask;
        }
    }

    private async Task<IConnectionMultiplexer?> ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var configuration = ConfigurationOptions.Parse(_options.ConnectionString, true);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectRetry = _options.ConnectRetry;
            configuration.ConnectTimeout = _options.ConnectTimeoutMilliseconds;
            configuration.SyncTimeout = _options.SyncTimeoutMilliseconds;
            return await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to connect to Redis for advanced cache operations.");
            return null;
        }
    }
}
