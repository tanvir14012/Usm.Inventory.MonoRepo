namespace Administration.Application.Departments.Dtos;

public sealed record DepartmentInput(
    string NameEn,
    string NameAr,
    string Code,
    Guid? ParentId,
    bool IsActive);
