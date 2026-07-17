using Administration.Application.Abstractions;
using Administration.Application.Common;
using Administration.Application.Departments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Contracts.Localization;

namespace Administration.Application.Departments.Commands;

public sealed record UpdateDepartmentCommand(Guid Id, DepartmentInput Department) : IRequest<DepartmentDto>;

public sealed class UpdateDepartmentCommandHandler(IAdministrationDbContext context)
    : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Departments.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new ApplicationValidationException("Department was not found.");

        var input = request.Department;
        var normalizedCode = input.Code.Trim().ToUpperInvariant();

        var duplicateExists = await context.Departments.AnyAsync(
            x => x.Id != request.Id && x.Code == normalizedCode,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ApplicationValidationException($"Department code '{normalizedCode}' already exists.");
        }

        entity.Update(
            new LocalizedText(En: input.NameEn.Trim(), Ar: input.NameAr.Trim()),
            normalizedCode,
            input.ParentId,
            input.IsActive);

        await context.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(entity.Id, entity.Name.En, entity.Name.Ar, entity.Code, entity.ParentId, entity.IsActive, entity.CreatedAt);
    }
}
