namespace Favilonia.Domain.Entities;

public class Period : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // Название, например "I четверть", "II семестр 2026"
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsDeleted { get; set; } = false;
}
