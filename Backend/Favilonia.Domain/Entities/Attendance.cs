namespace Favilonia.Domain.Entities;

public class Attendance : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    // Только дата занятия (время игнорируется). Уникален в связке StudentId+SubjectId+Date.
    public DateTime Date { get; set; }

    // Одно из значений AttendanceStatus: Present / Absent / Late / Excused
    public string Status { get; set; } = AttendanceStatus.Present;
}
