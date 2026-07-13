using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Usm.Shared.Reflection.AssemblyScanning;

public static class AssemblyScanningExtensions
{
    public static IServiceCollection ScanAssignableTo<TService>(this IServiceCollection services, params Assembly[] assemblies)
        where TService : class
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo<TService>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
