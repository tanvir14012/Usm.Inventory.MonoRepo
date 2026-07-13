using MediatR;
using Salvage.Application.Abstractions;
using Salvage.Domain.SalvageRecords;

namespace Salvage.Application.SalvageRecords.Queries;

public record GetSalvageRecordsQuery : IRequest<IReadOnlyList<SalvageRecordDto>>;

public record SalvageRecordDto(Guid Id, string RecordNumber, SalvageStatus Status, DateTimeOffset SalvageDate);

public sealed class GetSalvageRecordsQueryHandler(ISalvageDbContext context)
    : IRequestHandler<GetSalvageRecordsQuery, IReadOnlyList<SalvageRecordDto>>
{
    public Task<IReadOnlyList<SalvageRecordDto>> Handle(GetSalvageRecordsQuery request, CancellationToken cancellationToken)
    {
        var result = context.SalvageRecords
            .Select(x => new SalvageRecordDto(x.Id, x.RecordNumber, x.Status, x.SalvageDate))
            .ToList();
        return Task.FromResult<IReadOnlyList<SalvageRecordDto>>(result);
    }
}
