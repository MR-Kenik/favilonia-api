namespace Favilonia.API.Services;

// Заглушка: вместо реальной отправки письма выводит токен в лог.
// Чтобы подключить настоящий SMTP, реализуй IEmailService и замени регистрацию в Program.cs.
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetAsync(string toEmail, string fullName, string resetToken)
    {
        _logger.LogWarning(
            "[EMAIL STUB] Сброс пароля для {FullName} <{Email}>. Токен: {Token}",
            fullName, toEmail, resetToken);

        return Task.CompletedTask;
    }
}
