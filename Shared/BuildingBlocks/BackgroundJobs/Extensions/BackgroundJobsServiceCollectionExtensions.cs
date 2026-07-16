using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Abstractions;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Internal;
using Usm.Shared.BuildingBlocks.BackgroundJobs.Models;

namespace Usm.Shared.BuildingBlocks.BackgroundJobs.Extensions;

public static class BackgroundJobsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Excel background job framework:
    /// <list type="bullet">
    ///   <item><see cref="IJobRepository"/> (in-memory default)</item>
    ///   <item><see cref="IJobScheduler"/></item>
    ///   <item><see cref="ICurrentJobContext"/></item>
    ///   <item><see cref="ExcelJobBackgroundService"/> hosted service</item>
    ///   <item>All <see cref="IExcelJob"/> implementations found via Scrutor</item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional configuration root for binding <see cref="BackgroundJobOptions"/>.</param>
    /// <param name="additionalAssemblies">
    /// Extra assemblies to scan for <see cref="IExcelJob"/> implementations, in addition
    /// to all application-level dependencies discovered automatically by Scrutor.
    /// </param>
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        params Assembly[] additionalAssemblies)
    {
        // Options
        if (configuration is not null)
            services.Configure<BackgroundJobOptions>(
                configuration.GetSection(BackgroundJobOptions.SectionName));
        else
            services.AddOptions<BackgroundJobOptions>();

        // Core infrastructure
        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddScoped<JobContextHolder>();
        services.AddScoped<ICurrentJobContext, CurrentJobContext>();
        services.AddScoped<IJobScheduler, JobScheduler>();

        // Background service
        services.AddHostedService<ExcelJobBackgroundService>();

        // Discover IExcelJob implementations via Scrutor.
        // Filters out framework/runtime assemblies to keep scanning fast.
        services.Scan(scan =>
        {
            var selector = scan
                .FromApplicationDependencies(a =>
                    !IsFrameworkAssembly(a.FullName));

            if (additionalAssemblies.Length > 0)
                selector = scan.FromAssemblies(additionalAssemblies);

            selector
                .AddClasses(c => c.AssignableTo<IExcelJob>(), publicOnly: true)
                .AsSelf()
                .WithTransientLifetime();
        });

        return services;
    }

    /// <summary>
    /// Replaces the default in-memory <see cref="IJobRepository"/> with a custom
    /// durable implementation (e.g. EF Core). Call <em>after</em> <see cref="AddBackgroundJobs"/>.
    /// </summary>
    public static IServiceCollection UseJobRepository<TRepository>(
        this IServiceCollection services)
        where TRepository : class, IJobRepository
    {
        // Remove the previously registered singleton and replace it.
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IJobRepository));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddSingleton<IJobRepository, TRepository>();
        return services;
    }

    private static bool IsFrameworkAssembly(string? fullName)
    {
        if (fullName is null) return true;
        return fullName.StartsWith("Microsoft.", StringComparison.Ordinal)
            || fullName.StartsWith("System.",    StringComparison.Ordinal)
            || fullName.StartsWith("mscorlib",   StringComparison.Ordinal)
            || fullName.StartsWith("netstandard",StringComparison.Ordinal);
    }
}
