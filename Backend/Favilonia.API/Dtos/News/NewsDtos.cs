using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.News;

public class CreateNewsRequest
{
    [Required(ErrorMessage = "Заголовок новости обязателен.")]
    [MaxLength(200, ErrorMessage = "Заголовок новости не может быть длиннее 200 символов.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Содержимое новости обязательно.")]
    public string Content { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }
}

public class UpdateNewsRequest
{
    [Required(ErrorMessage = "Заголовок новости обязателен.")]
    [MaxLength(200, ErrorMessage = "Заголовок новости не может быть длиннее 200 символов.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Содержимое новости обязательно.")]
    public string Content { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }
}

public class NewsResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
