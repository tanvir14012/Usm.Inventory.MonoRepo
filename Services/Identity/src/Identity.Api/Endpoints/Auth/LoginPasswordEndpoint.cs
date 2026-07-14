using Identity.Application.Auth.Commands;
using Identity.Infrastructure;
using MediatR;
using OpenIddict.Server.AspNetCore;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Identity.Api.Endpoints.Auth;

public sealed class LoginPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/login/password", Handle)
            .WithName("LoginPassword")
            .WithTags("Auth")
            .AllowAnonymous();
    }

    private static async Task<IResult> Handle(
        LoginPasswordCommand command,
        ISender sender,
        IClaimsPrincipalFactory claimsFactory)
    {
        var result = await sender.Send(command);

        if (result is null)
            return Results.Unauthorized();

        var principal = claimsFactory.Create(result);

        return Results.SignIn(
            principal,
            null,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}