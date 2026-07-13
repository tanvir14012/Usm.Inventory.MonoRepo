using MediatR;
using RepairMaintenance.Application.Abstractions;
using RepairMaintenance.Domain.RepairOrders;

namespace RepairMaintenance.Application.RepairOrders.Queries;

public record GetRepairOrdersQuery : IRequest<IReadOnlyList<RepairOrderDto>>;

public record RepairOrderDto(Guid Id, string OrderNumber, RepairOrderStatus Status, DateTimeOffset ReportedDate);

public sealed class GetRepairOrdersQueryHandler(IRepairMaintenanceDbContext context)
    : IRequestHandler<GetRepairOrdersQuery, IReadOnlyList<RepairOrderDto>>
{
    public Task<IReadOnlyList<RepairOrderDto>> Handle(GetRepairOrdersQuery request, CancellationToken cancellationToken)
    {
        var result = context.RepairOrders
            .Select(x => new RepairOrderDto(x.Id, x.OrderNumber, x.Status, x.ReportedDate))
            .ToList();
        return Task.FromResult<IReadOnlyList<RepairOrderDto>>(result);
    }
}
