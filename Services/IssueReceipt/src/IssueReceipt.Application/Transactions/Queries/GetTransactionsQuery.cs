using IssueReceipt.Application.Abstractions;
using MediatR;

namespace IssueReceipt.Application.Transactions.Queries;

public record GetTransactionsQuery : IRequest<IReadOnlyList<TransactionDto>>;

public record TransactionDto(Guid Id, string TransactionNumber, Guid InventoryItemId, decimal Quantity, DateTimeOffset Date);

public sealed class GetTransactionsQueryHandler(IIssueReceiptDbContext context)
    : IRequestHandler<GetTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    public Task<IReadOnlyList<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var result = context.IssueTransactions
            .Select(x => new TransactionDto(x.Id, x.TransactionNumber, x.InventoryItemId, x.Quantity, x.IssuedDate))
            .ToList();
        return Task.FromResult<IReadOnlyList<TransactionDto>>(result);
    }
}
