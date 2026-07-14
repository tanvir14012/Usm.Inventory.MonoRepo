using Identity.Application.Auth.Dtos;
using Identity.Application.Auth.Utils;
using Identity.Application.Users.Dtos;
using Identity.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Auth.Commands;

public sealed class LoginCacCommandHandler
    : IRequestHandler<LoginCacCommand, AuthenticatedUser?>
{
    private readonly IIdentityDbContext _db;
    private readonly ICertificateValidator _certificateValidator;
    private readonly ICertificateParser _certificateParser;

    public LoginCacCommandHandler(
        IIdentityDbContext db,
        ICertificateValidator certificateValidator,
        ICertificateParser certificateParser)
    {
        _db = db;
        _certificateValidator = certificateValidator;
        _certificateParser = certificateParser;
    }

    public async Task<AuthenticatedUser?> Handle(
        LoginCacCommand request,
        CancellationToken cancellationToken)
    {
        //
        // Validate certificate
        //
        if (!_certificateValidator.Validate(request.Certificate))
            return null;

        //
        // Extract DoD ID (EDIPI)
        //
        var dodId = _certificateParser.GetDodId(request.Certificate);

        //
        // Lookup credential
        //
        var credential = await _db.UserCredentials
            .SingleOrDefaultAsync(x =>
                x.Type == CredentialType.Cac &&
                x.Identifier == dodId &&
                x.IsEnabled,
                cancellationToken);

        if (credential is null)
            return null;

        //
        // Load user
        //
        var user = await _db.Users
            .SingleOrDefaultAsync(x =>
                x.Id == credential.UserId &&
                x.IsActive,
                cancellationToken);

        if (user is null)
            return null;

        return new AuthenticatedUser(
            user.Id,
            user.Username,
            user.Email,
            $"{user.FirstName} {user.LastName}");
    }
}