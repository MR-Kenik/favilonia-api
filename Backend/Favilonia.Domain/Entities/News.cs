namespace Favilonia.Domain.Entities;

public class News : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}