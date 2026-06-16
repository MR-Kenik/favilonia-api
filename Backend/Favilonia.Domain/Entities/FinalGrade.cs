namespace Favilonia.Domain.Entities;

public class FinalGrade : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    // Итоговая оценка привязана к конкретному периоду (четверти)
    public Guid PeriodId { get; set; }
    public Period Period { get; set; } = null!;

    // Итоговая оценка по шкале 1–5
    public int Value { get; set; }

    public string? Comment { get; set; }
}
