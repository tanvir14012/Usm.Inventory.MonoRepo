using Administration.Application.Abstractions;
using Administration.Application.Common;
using Administration.Application.Departments.Dtos;
using Administration.Domain.Departments;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Usm.Shared.Contracts.Localization;

namespace Administration.Application.Departments.Commands;

public sealed record CreateDepartmentCommand(DepartmentInput Department) : IRequest<DepartmentDto>;

public sealed class CreateDepartmentCommandHandler(IAdministrationDbContext context)
    : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var input = request.Department;
        var normalizedCode = input.Code.Trim().ToUpperInvariant();

        if (await context.Departments.AnyAsync(x => x.Code == normalizedCode, cancellationToken))
        {
            throw new ApplicationValidationException($"Department code '{normalizedCode}' already exists.");
        }

        var entity = Department.Create(
            new LocalizedText(En: input.NameEn.Trim(), Ar: input.NameAr.Trim()),
            normalizedCode,
            input.ParentId);

        entity.Update(entity.Name, normalizedCode, input.ParentId, input.IsActive);

        context.Departments.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(entity.Id, entity.Name.En, entity.Name.Ar, entity.Code, entity.ParentId, entity.IsActive, entity.CreatedAt);
    }
}
