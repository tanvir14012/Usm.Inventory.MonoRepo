using Identity.Application.Users.Dtos;
using Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Auth.Commands
{
    public sealed class LoginPasswordCommandHandler : IRequestHandler<LoginPasswordCommand, AuthenticatedUser?>
    {
        private readonly IIdentityDbContext _db;
        private readonly PasswordHasher<User> _passwordHasher;

        public LoginPasswordCommandHandler(IIdentityDbContext db, PasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthenticatedUser?> Handle(LoginPasswordCommand request, CancellationToken cancellationToken)
        {
            var credential = await _db.UserCredentials
                .SingleOrDefaultAsync(x =>
                    x.Type == CredentialType.Password &&
                    x.Identifier == request.Username &&
                    x.IsEnabled,
                    cancellationToken);

            if (credential is null)
                return null;

            var metadata = credential.GetMetadata<PasswordCredentialMetadata>();

            var result = _passwordHasher.VerifyHashedPassword(null, metadata.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed)
                return null;

            var user = await _db.Users.FindAsync([credential.UserId], cancellationToken);

            if (user is null || !user.IsActive)
                return null;

            return new AuthenticatedUser(user.Id, user.Username, user.Email, $"{user.FirstName} {user.LastName}");
        }
    }
}
