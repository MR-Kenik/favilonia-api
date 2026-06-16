using System.ComponentModel.DataAnnotations;
using Favilonia.API.Validation;

namespace Favilonia.API.Dtos.Grades;

public class CreateGradeRequest
{
    [Required(ErrorMessage = "Студент обязателен.")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "Учитель обязателен.")]
    public Guid TeacherId { get; set; }

    [Required(ErrorMessage = "Предмет обязателен.")]
    public Guid SubjectId { get; set; }

    [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5.")]
    public int Value { get; set; }

    [Required(ErrorMessage = "Тип оценки обязателен.")]
    [AllowedGradeType(ErrorMessage = "Недопустимый тип оценки.")]
    public string GradeType { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Комментарий не может быть длиннее 500 символов.")]
    public string? Comment { get; set; }

    [Required(ErrorMessage = "Дата выставления обязательна.")]
    public DateTime GradedAt { get; set; }
}

public class UpdateGradeRequest
{
    [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5.")]
    public int Value { get; set; }

    [Required(ErrorMessage = "Тип оценки обязателен.")]
    [AllowedGradeType(ErrorMessage = "Недопустимый тип оценки.")]
    public string GradeType { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Комментарий не может быть длиннее 500 символов.")]
    public string? Comment { get; set; }

    [Required(ErrorMessage = "Дата выставления обязательна.")]
    public DateTime GradedAt { get; set; }
}

public class GradeSummaryResponse
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public double AverageGrade { get; set; }
    public int GradeCount { get; set; }
    public int AttendanceTotal { get; set; }
    public int AttendancePresent { get; set; }   // Present + Late
    public double AttendancePercent { get; set; }
}

public class GradeResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid? PeriodId { get; set; }
    public string? PeriodName { get; set; }
    public int Value { get; set; }
    public string GradeType { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime GradedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
