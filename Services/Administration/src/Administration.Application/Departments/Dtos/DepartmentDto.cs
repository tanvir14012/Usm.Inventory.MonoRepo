namespace Administration.Application.Departments.Dtos;

public sealed record DepartmentDto(
    Guid Id,
    string NameEn,
    string NameAr,
    string Code,
    Guid? ParentId,
    bool IsActive,
    DateTimeOffset CreatedAt);
