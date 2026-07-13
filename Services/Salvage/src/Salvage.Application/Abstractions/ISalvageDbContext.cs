using Salvage.Domain.SalvageRecords;

namespace Salvage.Application.Abstractions;

public interface ISalvageDbContext
{
    IQueryable<SalvageRecord> SalvageRecords { get; }
}
