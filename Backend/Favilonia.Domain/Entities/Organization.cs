namespace Favilonia.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;

    public ICollection<News> News { get; set; } = new List<News>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<User> Users { get; set; } = new List<User>();
}