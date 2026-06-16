using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.Pages;

public class CreatePageRequest
{
    [Required(ErrorMessage = "Название страницы обязательно.")]
    [MaxLength(200, ErrorMessage = "Название страницы не может быть длиннее 200 символов.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug страницы обязателен.")]
    [MaxLength(200, ErrorMessage = "Slug не может быть длиннее 200 символов.")]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug должен содержать только букв (a-z), цифры (0-9) и дефис (-).")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Содержимое страницы обязательно.")]
    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = false;
}

public class UpdatePageRequest
{
    [Required(ErrorMessage = "Название страницы обязательно.")]
    [MaxLength(200, ErrorMessage = "Название страницы не может быть длиннее 200 символов.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug страницы обязателен.")]
    [MaxLength(200, ErrorMessage = "Slug не может быть длиннее 200 символов.")]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug должен содержать только букв (a-z), цифры (0-9) и дефис (-).")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Содержимое страницы обязательно.")]
    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = false;
}

public class PageResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
