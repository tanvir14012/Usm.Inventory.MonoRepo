using Fido2NetLib;
using Fido2NetLib.Objects;
using Identity.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;

namespace Identity.Application.Auth.Commands;

public sealed class BeginFido2LoginCommandHandler(
    IIdentityDbContext db,
    Fido2 fido2)
    : IRequestHandler<BeginFido2LoginCommand, string>
{
    public async Task<string> Handle(
        BeginFido2LoginCommand request,
        CancellationToken ct)
    {
        var credentials = await db.UserCredentials
            .Where(x =>
                x.Type == CredentialType.Fido2 &&
                x.IsEnabled)
            .Select(x => new PublicKeyCredentialDescriptor(
                Base64Url.DecodeFromChars(x.Identifier)))
            .ToListAsync(ct);

        var options = fido2.GetAssertionOptions(
            credentials,
            UserVerificationRequirement.Required);

        return options.ToJson();
    }
}