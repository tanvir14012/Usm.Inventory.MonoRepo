using MediatR;
using Microsoft.EntityFrameworkCore;
using RepairMaintenance.Application.Abstractions;
using RepairMaintenance.Domain.RepairOrders;

namespace RepairMaintenance.Application.RepairOrders.Queries;

public record GetRepairOrdersQuery : IRequest<IReadOnlyList<RepairOrderDto>>;

public record RepairOrderDto(
    Guid Id,
    string OrderNumber,
    string Description,
    RepairOrderStatus Status,
    DateTimeOffset ReportedDate,
    DateTimeOffset? CompletedDate);

public sealed class GetRepairOrdersQueryHandler(IRepairMaintenanceDbContext context)
    : IRequestHandler<GetRepairOrdersQuery, IReadOnlyList<RepairOrderDto>>
{
    public async Task<IReadOnlyList<RepairOrderDto>> Handle(GetRepairOrdersQuery request, CancellationToken cancellationToken)
    {
        var result = await context.RepairOrders
            .OrderByDescending(x => x.ReportedDate)
            .Select(x => new RepairOrderDto(
                x.Id,
                x.OrderNumber,
                x.Description,
                x.Status,
                x.ReportedDate,
                x.CompletedDate))
            .ToListAsync(cancellationToken);

        return result;
    }
}
