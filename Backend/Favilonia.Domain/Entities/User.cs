namespace Favilonia.Domain.Entities;

public class User : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    // Студент принадлежит ровно одной группе (null — не распределён)
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
}
