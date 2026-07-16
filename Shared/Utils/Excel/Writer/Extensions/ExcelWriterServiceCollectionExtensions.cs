using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Utils.Excel.Writer.Abstractions;
using Usm.Shared.Utils.Excel.Writer.Internal;

namespace Usm.Shared.Utils.Excel.Writer.Extensions;

public static class ExcelWriterServiceCollectionExtensions
{
    /// <summary>
    /// Registers the generic <see cref="IDtoPopulator{TDto}"/> with cached compiled setters.
    /// </summary>
    public static IServiceCollection AddExcelWriter(this IServiceCollection services)
    {
        services.AddTransient(typeof(IDtoPopulator<>), typeof(DtoPopulator<>));
        return services;
    }
}
