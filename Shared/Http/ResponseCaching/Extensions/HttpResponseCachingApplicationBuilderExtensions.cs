using Microsoft.AspNetCore.Builder;
using Usm.Shared.Http.ResponseCaching.Middleware;

namespace Usm.Shared.Http.ResponseCaching.Extensions;

public static class HttpResponseCachingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHttpResponseCaching(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HttpResponseCachingMiddleware>();
    }
}
