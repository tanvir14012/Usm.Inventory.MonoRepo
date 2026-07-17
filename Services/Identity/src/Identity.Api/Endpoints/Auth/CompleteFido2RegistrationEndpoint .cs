using Identity.Application.Auth.Commands;
using Identity.Application.Auth.Dtos;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Identity.Api.Endpoints.Auth;

public sealed class CompleteFido2RegistrationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/fido2/register", Handle)
            .AllowAnonymous()
            .WithTags("Auth");
    }

    private static async Task<IResult> Handle(
        CompleteFido2RegistrationRequest request,
        ISender sender)
    {
        await sender.Send(
            new CompleteFido2RegistrationCommand(
                request.AttestationResponse,
                request.AttestationOptionsJson));

        return Results.Ok();
    }
}