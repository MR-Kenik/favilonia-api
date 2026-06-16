using Favilonia.API.Dtos.Auth;
using Favilonia.API.Services;
using Favilonia.API.Settings;
using Favilonia.Domain.Entities;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly IEmailService _emailService;
    private readonly int _expirationMinutes;

    public AuthController(
        AppDbContext db,
        JwtTokenGenerator tokenGenerator,
        RefreshTokenService refreshTokenService,
        IEmailService emailService,
        IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _tokenGenerator = tokenGenerator;
        _refreshTokenService = refreshTokenService;
        _emailService = emailService;
        _expirationMinutes = jwtOptions.Value.ExpirationMinutes;
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.OrganizationId == request.OrganizationId && x.Email == request.Email);
        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Неверные учетные данные." });
        }

        var refreshToken = _refreshTokenService.Generate(user.Id);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return Ok(BuildAuthResponse(user, refreshToken));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request)
    {
        // Include(User) нужен, чтобы сразу получить данные пользователя для генерации нового access-токена.
        var storedToken = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            return Unauthorized(new { Message = "Недействительный или истёкший refresh-токен." });
        }

        // Ротация: старый refresh-токен отзывается, выдаётся новый.
        // ReplacedByToken позволяет восстановить цепочку, если нужно расследовать утечку.
        var newRefreshToken = _refreshTokenService.Generate(storedToken.UserId);

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken.Token;

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync();

        return Ok(BuildAuthResponse(storedToken.User, newRefreshToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

        if (storedToken is { IsRevoked: false })
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    // Всегда возвращает 200 — даже если email не найден, чтобы не раскрывать факт регистрации.
    [HttpPost("forgot-password")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x =>
            x.OrganizationId == request.OrganizationId &&
            x.Email == request.Email);

        if (user is not null)
        {
            // Инвалидируем все предыдущие токены сброса для этого пользователя.
            var oldTokens = await _db.PasswordResetTokens
                .Where(x => x.UserId == user.Id && !x.IsUsed)
                .ToListAsync();
            foreach (var old in oldTokens)
                old.IsUsed = true;

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            _db.PasswordResetTokens.Add(resetToken);
            await _db.SaveChangesAsync();

            await _emailService.SendPasswordResetAsync(user.Email, user.FullName, resetToken.Token);
        }

        return Ok(new { Message = "Если аккаунт существует, инструкции по сбросу пароля отправлены на email." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var resetToken = await _db.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.Token);

        if (resetToken is null || !resetToken.IsValid)
            return BadRequest(new { Message = "Токен недействителен или истёк." });

        resetToken.User.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        resetToken.IsUsed = true;

        // После смены пароля отзываем все refresh-токены — принудительный повторный вход.
        var refreshTokens = await _db.RefreshTokens
            .Where(x => x.UserId == resetToken.UserId && !x.IsRevoked)
            .ToListAsync();
        foreach (var rt in refreshTokens)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Пароль успешно изменён. Войдите заново." });
    }

    private AuthResponse BuildAuthResponse(User user, RefreshToken refreshToken)
    {
        var accessToken = _tokenGenerator.GenerateToken(user);

        return new AuthResponse
        {
            Token = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}
