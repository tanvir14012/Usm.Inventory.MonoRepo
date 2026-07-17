using Fido2NetLib;
using Identity.Application.Users.Dtos;
using Identity.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using System.Text.Json;

namespace Identity.Application.Auth.Commands;

public sealed class CompleteFido2LoginCommandHandler(
    IIdentityDbContext db,
    IFido2 fido2)
    : IRequestHandler<CompleteFido2LoginCommand, AuthenticatedUser?>
{
    public async Task<AuthenticatedUser?> Handle(
        CompleteFido2LoginCommand request,
        CancellationToken ct)
    {
        var assertionOptions = AssertionOptions.FromJson(request.AssertionOptionsJson);
        var credentialId = request.AssertionResponse.Id;

        var credential = await db.UserCredentials
            .SingleOrDefaultAsync(x =>
                x.Type == CredentialType.Fido2 &&
                x.Identifier == credentialId &&
                x.IsEnabled,
                ct);

        if (credential is null)
            return null;

        var metadata = credential.GetMetadata<Fido2CredentialMetadata>();

        var result = await fido2.MakeAssertionAsync(
            new MakeAssertionParams
            {
                AssertionResponse = request.AssertionResponse,
                OriginalOptions = assertionOptions,
                StoredPublicKey = metadata.PublicKey,
                StoredSignatureCounter = metadata.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = async (args, cancellationToken) =>
                {
                    if (args.UserHandle is null || args.UserHandle.Length == 0)
                        return false;

                    var userId = new Guid(args.UserHandle);
                    var expectedCredentialId = Base64Url.EncodeToString(args.CredentialId);

                    return await db.UserCredentials.AnyAsync(x =>
                        x.UserId == userId &&
                        x.Type == CredentialType.Fido2 &&
                        x.Identifier == expectedCredentialId &&
                        x.IsEnabled,
                        cancellationToken);
                }
            },
            cancellationToken: ct);

        var updatedMetadata = metadata with
        {
            SignatureCounter = result.SignCount
        };

        credential.UpdateMetadata(JsonSerializer.Serialize(updatedMetadata));

        var user = await db.Users
            .SingleOrDefaultAsync(x =>
                x.Id == credential.UserId &&
                x.IsActive,
                ct);

        if (user is null)
            return null;

        await db.SaveChangesAsync(ct);

        return new AuthenticatedUser(
            user.Id,
            user.Username,
            user.Email,
            $"{user.FirstName} {user.LastName}");
    }
}
