using Administration.Application.Abstractions;
using Administration.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Administration.Application.Departments.Commands;

public sealed record DeleteDepartmentCommand(Guid Id) : IRequest;

public sealed class DeleteDepartmentCommandHandler(IAdministrationDbContext context)
    : IRequestHandler<DeleteDepartmentCommand>
{
    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Departments.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new ApplicationValidationException("Department was not found.");

        var hasChildren = await context.Departments.AnyAsync(x => x.ParentId == request.Id, cancellationToken);
        if (hasChildren)
        {
            throw new ApplicationValidationException("Department cannot be deleted while it still has child departments.");
        }

        context.Departments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
