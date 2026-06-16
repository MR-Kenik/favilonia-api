namespace Favilonia.Domain.Entities;

public static class AttendanceStatus
{
    public const string Present = "Present";   // Присутствовал
    public const string Absent  = "Absent";    // Отсутствовал
    public const string Late    = "Late";      // Опоздал
    public const string Excused = "Excused";   // По уважительной причине

    public static readonly IReadOnlyCollection<string> All = new[] { Present, Absent, Late, Excused };
}
