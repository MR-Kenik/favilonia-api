using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Идентификатор организации обязателен.")]
    public Guid OrganizationId { get; set; }

    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Неверный формат email.")]
    [MaxLength(200, ErrorMessage = "Email не может быть длиннее 200 символов.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов.")]
    [MaxLength(100, ErrorMessage = "Пароль не может быть длиннее 100 символов.")]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh-токен обязателен.")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Идентификатор организации обязателен.")]
    public Guid OrganizationId { get; set; }

    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Неверный формат email.")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Токен обязателен.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен.")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов.")]
    [MaxLength(100, ErrorMessage = "Пароль не может быть длиннее 100 символов.")]
    public string NewPassword { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
