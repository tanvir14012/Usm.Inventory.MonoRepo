using IssueReceipt.Application.Transactions.Queries;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace IssueReceipt.Api.Endpoints.Transactions;

public sealed class TransactionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/transactions", Handle)
            .WithName("GetTransactions")
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
        => Results.Ok(await sender.Send(new GetTransactionsQuery(), cancellationToken));
}
