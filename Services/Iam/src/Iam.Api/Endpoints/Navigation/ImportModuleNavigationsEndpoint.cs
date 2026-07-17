using Iam.Application.Common;
using Iam.Application.Navigation.Commands;
using Iam.Application.Navigation.Dtos;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Navigation;

public sealed class ImportModuleNavigationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/navigation/modules/import", Handle)
            .WithName("ImportModuleNavigations")
            .WithTags("Navigation")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ImportModuleNavigationsInput input,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new ImportModuleNavigationsCommand(input), cancellationToken);
            return Results.Ok(result);
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
