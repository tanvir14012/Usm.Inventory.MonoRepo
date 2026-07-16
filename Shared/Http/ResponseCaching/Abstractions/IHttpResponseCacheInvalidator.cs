namespace Usm.Shared.Http.ResponseCaching.Abstractions;

public interface IHttpResponseCacheInvalidator
{
    Task InvalidateByRouteAsync(string routePrefix, CancellationToken cancellationToken = default);
    Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}
