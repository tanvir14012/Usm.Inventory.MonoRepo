using IssueReceipt.Application.Abstractions;
using IssueReceipt.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Data.DbContextExtensions;

namespace IssueReceipt.Infrastructure.Persistence;

public class IssueReceiptDbContext(DbContextOptions<IssueReceiptDbContext> options)
    : ServiceDbContext(options, "issuereceipt"), IIssueReceiptDbContext
{
    public DbSet<IssueTransaction> IssueTransactions => Set<IssueTransaction>();
    public DbSet<ReceiptTransaction> ReceiptTransactions => Set<ReceiptTransaction>();
}
