using IssueReceipt.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IssueReceipt.Application.Transactions.Queries;

public record GetTransactionsQuery : IRequest<IReadOnlyList<TransactionDto>>;

public record TransactionDto(
    Guid Id,
    string TransactionNumber,
    string TransactionType,
    Guid InventoryItemId,
    Guid WarehouseId,
    decimal Quantity,
    string Counterparty,
    DateTimeOffset Date,
    string? Remarks);

public sealed class GetTransactionsQueryHandler(IIssueReceiptDbContext context)
    : IRequestHandler<GetTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    public async Task<IReadOnlyList<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var issueTransactions = await context.IssueTransactions
            .Select(x => new TransactionDto(
                x.Id,
                x.TransactionNumber,
                "Issue",
                x.InventoryItemId,
                x.WarehouseId,
                x.Quantity,
                x.IssuedTo,
                x.IssuedDate,
                x.Purpose))
            .ToListAsync(cancellationToken);

        var receiptTransactions = await context.ReceiptTransactions
            .Select(x => new TransactionDto(
                x.Id,
                x.TransactionNumber,
                "Receipt",
                x.InventoryItemId,
                x.WarehouseId,
                x.Quantity,
                x.ReceivedFrom,
                x.ReceivedDate,
                x.Notes))
            .ToListAsync(cancellationToken);

        return issueTransactions
            .Concat(receiptTransactions)
            .OrderByDescending(x => x.Date)
            .ToArray();
    }
}
