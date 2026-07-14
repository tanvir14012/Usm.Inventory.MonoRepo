using Identity.Application.Auth.Dtos;
using Identity.Application.Users.Dtos;
using MediatR;

namespace Identity.Application.Auth.Commands;

public sealed record LoginCacCommand(ClientCertificate Certificate) : IRequest<AuthenticatedUser?>;