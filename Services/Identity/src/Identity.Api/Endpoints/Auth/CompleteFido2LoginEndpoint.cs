using Identity.Application.Auth.Commands;
using Identity.Application.Auth.Dtos;
using Identity.Infrastructure;
using MediatR;
using OpenIddict.Server.AspNetCore;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Identity.Api.Endpoints.Auth;

public sealed class CompleteFido2LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/login/fido2", Handle)
            .AllowAnonymous()
            .WithTags("Auth");
    }

    private static async Task<IResult> Handle(
        CompleteFido2LoginRequest request,
        ISender sender,
        IClaimsPrincipalFactory claimsFactory)
    {
        var result = await sender.Send(
            new CompleteFido2LoginCommand(
                request.AssertionResponse,
                request.AssertionOptionsJson));

        if (result is null)
            return Results.Unauthorized();

        var principal = claimsFactory.Create(result);

        return Results.SignIn(
            principal,
            null,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
