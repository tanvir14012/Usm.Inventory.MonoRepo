using StackExchange.Redis;

namespace Usm.Shared.Caching.Internal;

internal interface IRedisConnectionAccessor
{
    Task<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken = default);
}
