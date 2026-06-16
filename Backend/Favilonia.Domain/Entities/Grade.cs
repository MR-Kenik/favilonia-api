namespace Favilonia.Domain.Entities;

public class Grade : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    // Оценка по шкале 1–5
    public int Value { get; set; }

    // Тип работы: ControlWork / Homework / OralAnswer / Test / Other
    public string GradeType { get; set; } = Entities.GradeType.Other;

    public string? Comment { get; set; }

    public DateTime GradedAt { get; set; }

    // К какому учебному периоду (четверти) относится оценка — опционально
    public Guid? PeriodId { get; set; }
    public Period? Period { get; set; }
}
