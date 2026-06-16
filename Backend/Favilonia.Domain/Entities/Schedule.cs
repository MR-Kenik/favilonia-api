namespace Favilonia.Domain.Entities;

public class Schedule : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsDeleted { get; set; } = false;
}
