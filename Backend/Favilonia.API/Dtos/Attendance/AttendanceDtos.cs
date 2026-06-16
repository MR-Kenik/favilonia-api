using System.ComponentModel.DataAnnotations;
using Favilonia.API.Validation;

namespace Favilonia.API.Dtos.Attendance;

public class CreateAttendanceRequest
{
    [Required(ErrorMessage = "Студент обязателен.")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "Учитель обязателен.")]
    public Guid TeacherId { get; set; }

    [Required(ErrorMessage = "Предмет обязателен.")]
    public Guid SubjectId { get; set; }

    [Required(ErrorMessage = "Дата занятия обязательна.")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Статус обязателен.")]
    [AllowedStatus(ErrorMessage = "Недопустимый статус посещаемости.")]
    public string Status { get; set; } = string.Empty;
}

public class UpdateAttendanceRequest
{
    [Required(ErrorMessage = "Статус обязателен.")]
    [AllowedStatus(ErrorMessage = "Недопустимый статус посещаемости.")]
    public string Status { get; set; } = string.Empty;
}

public class BulkAttendanceEntry
{
    [Required(ErrorMessage = "Студент обязателен.")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "Статус обязателен.")]
    [AllowedStatus(ErrorMessage = "Недопустимый статус посещаемости.")]
    public string Status { get; set; } = string.Empty;
}

public class BulkCreateAttendanceRequest
{
    [Required(ErrorMessage = "Учитель обязателен.")]
    public Guid TeacherId { get; set; }

    [Required(ErrorMessage = "Предмет обязателен.")]
    public Guid SubjectId { get; set; }

    [Required(ErrorMessage = "Дата занятия обязательна.")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Список записей обязателен.")]
    [MinLength(1, ErrorMessage = "Необходимо передать хотя бы одну запись.")]
    public List<BulkAttendanceEntry> Entries { get; set; } = new();
}

public class BulkAttendanceResponse
{
    public int Created { get; set; }
    public int Skipped { get; set; }   // уже существовавшие записи
    public List<AttendanceResponse> Items { get; set; } = new();
}

public class AttendanceResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
