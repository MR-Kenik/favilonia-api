using System.ComponentModel.DataAnnotations;
using Favilonia.API.Validation;

namespace Favilonia.API.Dtos.Users;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Неверный формат email.")]
    [MaxLength(200, ErrorMessage = "Email не может быть длиннее 200 символов.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов.")]
    [MaxLength(100, ErrorMessage = "Пароль не может быть длиннее 100 символов.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "ФИО обязательно.")]
    [MaxLength(200, ErrorMessage = "ФИО не может быть длиннее 200 символов.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Роль обязательна.")]
    [MaxLength(50, ErrorMessage = "Роль не может быть длиннее 50 символов.")]
    [AllowedRoles(ErrorMessage = "Роль пользователя недопустима.")]
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    [MaxLength(100, ErrorMessage = "Пароль не может быть длиннее 100 символов.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "ФИО обязательно.")]
    [MaxLength(200, ErrorMessage = "ФИО не может быть длиннее 200 символов.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Роль обязательна.")]
    [MaxLength(50, ErrorMessage = "Роль не может быть длиннее 50 символов.")]
    [AllowedRoles(ErrorMessage = "Роль пользователя недопустима.")]
    public string Role { get; set; } = string.Empty;
}

public class UpdateMeRequest
{
    [Required(ErrorMessage = "ФИО обязательно.")]
    [MaxLength(200, ErrorMessage = "ФИО не может быть длиннее 200 символов.")]
    public string FullName { get; set; } = string.Empty;

    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов.")]
    [MaxLength(100, ErrorMessage = "Пароль не может быть длиннее 100 символов.")]
    public string? NewPassword { get; set; }
}

public class UserResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// --- Импорт / экспорт студентов ---

public class ImportCheckRegisteredItem
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? GroupName { get; set; }
}

public class ImportCheckUnregisteredItem
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ImportCheckResponse
{
    public int TotalInFile { get; set; }
    public int RegisteredCount { get; set; }
    public int UnregisteredCount { get; set; }
    public List<ImportCheckRegisteredItem> Registered { get; set; } = new();
    public List<ImportCheckUnregisteredItem> Unregistered { get; set; } = new();
}
