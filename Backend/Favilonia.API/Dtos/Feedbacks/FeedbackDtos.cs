using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.Feedbacks;

public class CreateFeedbackRequest
{
    [Required(ErrorMessage = "Имя обязательно.")]
    [MaxLength(200, ErrorMessage = "Имя не может быть длиннее 200 символов.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Неверный формат email.")]
    [MaxLength(200, ErrorMessage = "Email не может быть длиннее 200 символов.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Сообщение обязательно.")]
    [MinLength(10, ErrorMessage = "Сообщение должно содержать минимум 10 символов.")]
    [MaxLength(5000, ErrorMessage = "Сообщение не может быть длиннее 5000 символов.")]
    public string Message { get; set; } = string.Empty;
}

public class FeedbackResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
