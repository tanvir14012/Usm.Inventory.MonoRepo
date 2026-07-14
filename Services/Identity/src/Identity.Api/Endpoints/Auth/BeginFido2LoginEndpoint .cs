using Identity.Application.Auth.Commands;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Identity.Api.Endpoints.Auth;

public sealed class BeginFido2LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/login/fido2/options", Handle)
            .AllowAnonymous()
            .WithTags("Auth");
    }

    private static async Task<IResult> Handle(ISender sender)
    {
        var json = await sender.Send(new BeginFido2LoginCommand());

        return Results.Text(json, "application/json");
    }
}