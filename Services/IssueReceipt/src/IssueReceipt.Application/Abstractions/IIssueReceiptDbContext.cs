using IssueReceipt.Domain.Transactions;

namespace IssueReceipt.Application.Abstractions;

public interface IIssueReceiptDbContext
{
    IQueryable<IssueTransaction> IssueTransactions { get; }
    IQueryable<ReceiptTransaction> ReceiptTransactions { get; }
}
