namespace Favilonia.API.Services;

public interface IEmailService
{
    /// <summary>
    /// Отправляет письмо со ссылкой/токеном для сброса пароля.
    /// </summary>
    Task SendPasswordResetAsync(string toEmail, string fullName, string resetToken);
}
