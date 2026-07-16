using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Utils.Excel.Reader.Abstractions;
using Usm.Shared.Utils.Excel.Reader.Internal;
using Usm.Shared.Utils.Excel.Writer.Extensions;

namespace Usm.Shared.Utils.Excel.Reader.Extensions;

public static class ExcelReaderServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IExcelReader"/> and its dependencies.
    /// Also calls <c>AddExcelWriter()</c> so <c>IDtoPopulator&lt;T&gt;</c> is available.
    /// </summary>
    public static IServiceCollection AddExcelReader(this IServiceCollection services)
    {
        services.AddExcelWriter();
        services.AddScoped<IExcelReader, OpenXmlExcelReader>();
        return services;
    }
}
