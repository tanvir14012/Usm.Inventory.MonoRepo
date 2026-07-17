using Iam.Application.Common;
using Iam.Application.Navigation.Commands;
using Iam.Application.Navigation.Dtos;
using Iam.Domain.Navigation;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Iam.Api.Endpoints.Navigation;

public sealed class CreateModuleNavigationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/navigation/modules", Handle)
            .WithName("CreateModuleNavigation")
            .WithTags("Navigation")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        CreateModuleNavigationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new CreateModuleNavigationCommand(request.BuildingBlockType, request.Module),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}

public sealed record CreateModuleNavigationRequest(
    MilitaryBuildingBlockType BuildingBlockType,
    ModuleNavigationInput Module);
