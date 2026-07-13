using Microsoft.Extensions.DependencyInjection;

namespace Usm.Shared.BuildingBlocks.Grpc;

public static class GrpcExtensions
{
    public static IServiceCollection AddGrpcFederationSupport(this IServiceCollection services)
    {
        // Kept explicit and minimal to avoid hard runtime assumptions in initial scaffold.
        return services;
    }
}
