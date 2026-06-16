using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.FinalGrades;

public class CreateFinalGradeRequest
{
    [Required(ErrorMessage = "ID студента обязателен.")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "ID учителя обязателен.")]
    public Guid TeacherId { get; set; }

    [Required(ErrorMessage = "ID предмета обязателен.")]
    public Guid SubjectId { get; set; }

    [Required(ErrorMessage = "ID периода обязателен.")]
    public Guid PeriodId { get; set; }

    [Range(1, 5, ErrorMessage = "Итоговая оценка должна быть от 1 до 5.")]
    public int Value { get; set; }

    [MaxLength(500, ErrorMessage = "Комментарий не может быть длиннее 500 символов.")]
    public string? Comment { get; set; }
}

public class UpdateFinalGradeRequest
{
    [Range(1, 5, ErrorMessage = "Итоговая оценка должна быть от 1 до 5.")]
    public int Value { get; set; }

    [MaxLength(500, ErrorMessage = "Комментарий не может быть длиннее 500 символов.")]
    public string? Comment { get; set; }
}

public class FinalGradeResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int Value { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
