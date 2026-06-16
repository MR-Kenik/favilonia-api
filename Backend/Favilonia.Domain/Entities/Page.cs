namespace Favilonia.Domain.Entities;

public class Page : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = false;

    public bool IsDeleted { get; set; } = false;
}
