using Microsoft.Extensions.Options;
using Usm.Shared.Caching.Abstractions;
using Usm.Shared.Http.ResponseCaching.Abstractions;
using Usm.Shared.Http.ResponseCaching.Options;

namespace Usm.Shared.Http.ResponseCaching.Services;

public sealed class HttpResponseCacheInvalidator(
    ICacheService cacheService,
    IOptions<HttpResponseCachingOptions> options) : IHttpResponseCacheInvalidator
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly HttpResponseCachingOptions _options = options.Value;

    public Task InvalidateByRouteAsync(string routePrefix, CancellationToken cancellationToken = default)
    {
        var normalizedRoute = routePrefix.Trim().TrimStart('/');
        var pattern = $"{_options.KeyNamespace}:{normalizedRoute}*";
        return _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
    }

    public Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        return _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
    }
}
