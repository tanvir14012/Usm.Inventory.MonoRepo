using Administration.Domain.Common;
using Usm.Shared.Contracts.Localization;
using Usm.Shared.Data.DbContextExtensions;

namespace Administration.Domain.Departments;

public sealed record DepartmentCreatedDomainEvent(Guid DepartmentId, string Code) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public sealed class Department : AggregateRoot<Guid>, IAuditable
{
    public LocalizedText Name { get; private set; } = LocalizedText.Empty;
    public string Code { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }


    private Department() { }

    public static Department Create(LocalizedText name, string code, Guid? parentId = null)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            ParentId = parentId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        department.RaiseDomainEvent(new DepartmentCreatedDomainEvent(department.Id, code));
        return department;
    }

    public void Update(LocalizedText name, string code, Guid? parentId, bool isActive)
    {
        Name = name;
        Code = code.Trim();
        ParentId = parentId;
        IsActive = isActive;
    }
}
