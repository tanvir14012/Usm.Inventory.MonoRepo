using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;

namespace Usm.Shared.BuildingBlocks.Security.Mtls;

public static class MtlsExtensions
{
    public static IServiceCollection AddCertificateValidation(this IServiceCollection services, Func<X509Certificate2, bool> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        services.AddSingleton(validator);
        return services;
    }
}
