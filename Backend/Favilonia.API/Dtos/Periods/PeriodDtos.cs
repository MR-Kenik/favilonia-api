using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.Periods;

public class CreatePeriodRequest
{
    [Required(ErrorMessage = "Название периода обязательно.")]
    [MaxLength(100, ErrorMessage = "Название периода не может быть длиннее 100 символов.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Дата начала обязательна.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Дата окончания обязательна.")]
    public DateTime EndDate { get; set; }
}

public class UpdatePeriodRequest
{
    [Required(ErrorMessage = "Название периода обязательно.")]
    [MaxLength(100, ErrorMessage = "Название периода не может быть длиннее 100 символов.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Дата начала обязательна.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Дата окончания обязательна.")]
    public DateTime EndDate { get; set; }
}

public class PeriodResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
