using System.Security.Cryptography;
using Favilonia.API.Settings;
using Favilonia.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Favilonia.API.Services;

/// <summary>
/// Создаёт криптостойкие refresh-токены с заданным сроком жизни.
/// </summary>
public class RefreshTokenService
{
    private readonly JwtSettings _settings;

    public RefreshTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public RefreshToken Generate(Guid userId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays)
        };
    }
}
