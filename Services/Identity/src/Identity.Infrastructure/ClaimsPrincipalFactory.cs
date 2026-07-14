using Identity.Application.Users.Dtos;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Usm.Shared.Reflection.AssemblyScanning.ServiceLifetimeMarkers;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Infrastructure
{
    public interface IClaimsPrincipalFactory
    {
        ClaimsPrincipal Create(AuthenticatedUser user);
    }


    public sealed class ClaimsPrincipalFactory : IClaimsPrincipalFactory, ISingletonService
    {
        public ClaimsPrincipal Create(AuthenticatedUser user)
        {
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            identity.SetClaim(Claims.Subject, user.Id.ToString());
            identity.SetClaim(Claims.Name, user.Username);
            identity.SetClaim(Claims.Email, user.Email);
            identity.SetClaim(Claims.Nickname, user.Name);

            identity.SetDestinations(claim => claim.Type switch
            {
                Claims.Subject => [Destinations.AccessToken],
                Claims.Name => [Destinations.AccessToken],
                Claims.Email => [Destinations.AccessToken],
                _ => [Destinations.AccessToken]
            });

            return new ClaimsPrincipal(identity);
        }
    }
}
