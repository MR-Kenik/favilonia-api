namespace Favilonia.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Криптографически случайный токен, передаётся в ссылке сброса пароля
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    // Токен одноразовый — после использования помечается как использованный
    public bool IsUsed { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public bool IsValid => !IsUsed && ExpiresAt > DateTime.UtcNow;
}
