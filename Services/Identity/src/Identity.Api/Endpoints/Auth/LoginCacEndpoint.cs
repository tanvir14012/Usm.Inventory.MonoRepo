using Identity.Application.Auth.Commands;
using Identity.Application.Auth.Dtos;
using Identity.Infrastructure;
using MediatR;
using OpenIddict.Server.AspNetCore;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Identity.Api.Endpoints.Auth;

public sealed class LoginCacEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/login/cac", Handle)
            .WithName("LoginCac")
            .WithTags("Auth")
            .AllowAnonymous();
    }

    private static async Task<IResult> Handle(
        HttpContext context,
        ISender sender,
        IClaimsPrincipalFactory claimsFactory)
    {
        var certificate = await context.Connection.GetClientCertificateAsync();

        if (certificate is null)
            return Results.Unauthorized();

        var command = new LoginCacCommand(
            new ClientCertificate(
                Subject: certificate.Subject,
                Issuer: certificate.Issuer,
                Thumbprint: certificate.Thumbprint ?? string.Empty,
                RawData: certificate.RawData));

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