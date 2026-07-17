using Fido2NetLib;
using Fido2NetLib.Objects;
using Identity.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;

namespace Identity.Application.Auth.Commands;

public sealed class BeginFido2RegistrationCommandHandler(
    IIdentityDbContext db,
    IFido2 fido2)
    : IRequestHandler<BeginFido2RegistrationCommand, string>
{
    public async Task<string> Handle(
        BeginFido2RegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .SingleAsync(x => x.Id == request.UserId, cancellationToken);

        var existingCredentials = await db.UserCredentials
            .Where(x =>
                x.UserId == request.UserId &&
                x.Type == CredentialType.Fido2 &&
                x.IsEnabled)
            .Select(x =>
                new PublicKeyCredentialDescriptor(
                    Base64Url.DecodeFromChars(x.Identifier)))
            .ToListAsync(cancellationToken);

        var fidoUser = new Fido2User
        {
            Id = user.Id.ToByteArray(),
            Name = user.Username,
            DisplayName = user.Username
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None
        });

        return options.ToJson();
    }
}