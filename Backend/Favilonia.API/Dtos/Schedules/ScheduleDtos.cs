using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.Schedules;

public class CreateScheduleRequest : IValidatableObject
{
    [Required(ErrorMessage = "Название расписания обязательно.")]
    [MaxLength(200, ErrorMessage = "Название расписания не может быть длиннее 200 символов.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Описание расписания обязательно.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Дата начала обязательна.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Дата окончания обязательна.")]
    public DateTime EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate <= StartDate)
        {
            yield return new ValidationResult(
                "Дата окончания должна быть позже даты начала",
                new[] { nameof(EndDate), nameof(StartDate) });
        }
    }
}

public class UpdateScheduleRequest
{
    [Required(ErrorMessage = "Название расписания обязательно.")]
    [MaxLength(200, ErrorMessage = "Название расписания не может быть длиннее 200 символов.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Описание расписания обязательно.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Дата начала обязательна.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Дата окончания обязательна.")]
    public DateTime EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate <= StartDate)
        {
            yield return new ValidationResult(
                "Дата окончания должна быть позже даты начала",
                new[] { nameof(EndDate), nameof(StartDate) });
        }
    }
}

public class ScheduleResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
