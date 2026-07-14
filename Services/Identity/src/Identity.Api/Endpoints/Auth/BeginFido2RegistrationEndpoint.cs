using Identity.Application.Auth.Commands;
using Identity.Application.Auth.Dtos;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Identity.Api.Endpoints.Auth;

public sealed class BeginFido2RegistrationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/fido2/register/options", Handle)
            .AllowAnonymous()
            .WithTags("Auth");
    }

    private static async Task<IResult> Handle(
        BeginFido2RegistrationRequest request,
        ISender sender)
    {
        var json = await sender.Send(
            new BeginFido2RegistrationCommand(request.UserId));

        return Results.Text(json, "application/json");
    }
}