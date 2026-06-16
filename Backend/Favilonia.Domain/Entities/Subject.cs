namespace Favilonia.Domain.Entities;

public class Subject : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;
}
