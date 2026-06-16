using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.Organizations;

public class CreateOrganizationRequest
{
    [Required(ErrorMessage = "Название организации обязательно.")]
    [MaxLength(200, ErrorMessage = "Название организации не может быть длиннее 200 символов.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Домен организации обязателен.")]
    [MaxLength(200, ErrorMessage = "Домен организации не может быть длиннее 200 символов.")]
    public string Domain { get; set; } = string.Empty;
}

public class UpdateOrganizationRequest
{
    [Required(ErrorMessage = "Название организации обязательно.")]
    [MaxLength(200, ErrorMessage = "Название организации не может быть длиннее 200 символов.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Домен организации обязателен.")]
    [MaxLength(200, ErrorMessage = "Домен организации не может быть длиннее 200 символов.")]
    public string Domain { get; set; } = string.Empty;
}

public class OrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// SaaS-онбординг: создание новой организации вместе с её первым администратором.
/// </summary>
public class RegisterOrganizationRequest
{
    [Required(ErrorMessage = "Название организации обязательно.")]
    [MaxLength(200, ErrorMessage = "Название организации не может быть длиннее 200 символов.")]
    public string OrganizationName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Домен организации обязателен.")]
    [MaxLength(200, ErrorMessage = "Домен организации не может быть длиннее 200 символов.")]
    public string Domain { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имя администратора обязательно.")]
    [MaxLength(200, ErrorMessage = "Имя администратора не может быть длиннее 200 символов.")]
    public string AdminFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email администратора обязателен.")]
    [EmailAddress(ErrorMessage = "Неверный формат email.")]
    [MaxLength(200, ErrorMessage = "Email не может быть длиннее 200 символов.")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль администратора обязателен.")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов.")]
    [MaxLength(100, ErrorMessage = "Пароль не может быть длиннее 100 символов.")]
    public string AdminPassword { get; set; } = string.Empty;
}
