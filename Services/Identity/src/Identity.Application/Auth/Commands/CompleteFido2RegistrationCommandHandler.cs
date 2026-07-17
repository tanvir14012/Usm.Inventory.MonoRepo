using Fido2NetLib;
using Identity.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using System.Text.Json;

namespace Identity.Application.Auth.Commands;

public sealed class CompleteFido2RegistrationCommandHandler(
    IIdentityDbContext db,
    IFido2 fido2)
    : IRequestHandler<CompleteFido2RegistrationCommand>
{
    public async Task Handle(
        CompleteFido2RegistrationCommand request,
        CancellationToken ct)
    {
        var options = CredentialCreateOptions.FromJson(request.AttestationOptionsJson);

        var credential = await fido2.MakeNewCredentialAsync(
            new MakeNewCredentialParams
            {
                AttestationResponse = request.AttestationResponse,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = async (args, cancellationToken) =>
                {
                    var base64Id = Base64Url.EncodeToString(args.CredentialId);
                    return !await db.UserCredentials.AnyAsync(
                        x => x.Identifier == base64Id,
                        cancellationToken);
                }
            },
            cancellationToken: ct);

        var userId = new Guid(options.User.Id);

        // Access the properties directly on the 'credential' variable
        var metadata = new Fido2CredentialMetadata(
            credential.Id,
            credential.PublicKey,
            credential.SignCount,
            credential.AaGuid,
            credential.Transports?.Select(x => x.ToString()).ToArray() ?? [],
            true);

        db.UserCredentials.Add(
            UserCredential.Create(
                userId,
                CredentialType.Fido2,
                Base64Url.EncodeToString(credential.Id),
                JsonSerializer.Serialize(metadata),
                "Passkey"));

        await db.SaveChangesAsync(ct);
    }
}