namespace Favilonia.Domain.Entities;

public class Group : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // Название класса/группы, например "10А" или "Группа 3"
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;

    public ICollection<User> Students { get; set; } = new List<User>();
}
