using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Auth;
using Favilonia.API.Dtos.Organizations;
using Favilonia.API.Services;
using Favilonia.API.Settings;
using Favilonia.Infrastructure.Data;
using Favilonia.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Favilonia.API.Extensions;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly int _expirationMinutes;

    public OrganizationsController(
        AppDbContext db,
        JwtTokenGenerator tokenGenerator,
        RefreshTokenService refreshTokenService,
        IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _tokenGenerator = tokenGenerator;
        _refreshTokenService = refreshTokenService;
        _expirationMinutes = jwtOptions.Value.ExpirationMinutes;
    }

    /// <summary>
    /// SaaS-онбординг: регистрирует новую организацию и её первого администратора,
    /// после чего сразу возвращает токены (авто-вход).
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("RegisterLimiter")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterOrganizationRequest request)
    {
        if (await _db.Organizations.AnyAsync(x => x.Domain == request.Domain))
        {
            return Conflict(new { Message = "Домен организации уже существует." });
        }

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.OrganizationName,
            Domain = request.Domain
        };

        var admin = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = request.AdminEmail,
            FullName = request.AdminFullName,
            PasswordHash = PasswordHasher.Hash(request.AdminPassword),
            Role = Roles.Admin
        };

        var refreshToken = _refreshTokenService.Generate(admin.Id);

        // Организация, админ и refresh-токен создаются в одной транзакции (один SaveChanges).
        _db.Organizations.Add(organization);
        _db.Users.Add(admin);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        var accessToken = _tokenGenerator.GenerateToken(admin);

        return Ok(new AuthResponse
        {
            Token = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            UserId = admin.Id,
            FullName = admin.FullName,
            Role = admin.Role
        });
    }

    // Список всех организаций видит только SuperAdmin (владелец платформы).
    // Обычный Admin видит только свою через GET /api/organizations/{id}.
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
    public async Task<ActionResult<IEnumerable<OrganizationResponse>>> GetAll()
    {
        var organizations = await _db.Organizations
            .OrderBy(x => x.Name)
            .Select(x => new OrganizationResponse
            {
                Id = x.Id,
                Name = x.Name,
                Domain = x.Domain,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(organizations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationResponse>> GetById(Guid id)
    {
        if (id != User.GetOrganizationId() && !User.IsInRole("SuperAdmin"))
        {
            return Forbid();
        }

        var organization = await _db.Organizations
            .Where(x => x.Id == id)
            .Select(x => new OrganizationResponse
            {
                Id = x.Id,
                Name = x.Name,
                Domain = x.Domain,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return NotFound();
        }

        return Ok(organization);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<OrganizationResponse>> Create(CreateOrganizationRequest request)
    {
        if (await _db.Organizations.AnyAsync(x => x.Domain == request.Domain))
        {
            return Conflict(new { Message = "Домен организации уже существует." });
        }

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Domain = request.Domain
        };

        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync();

        var response = new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            Domain = organization.Domain,
            CreatedAt = organization.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = organization.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid id, UpdateOrganizationRequest request)
    {
        var organization = await _db.Organizations.FindAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        if (organization.Id != User.GetOrganizationId())
        {
            return Forbid();
        }
        

        if (await _db.Organizations.AnyAsync(x => x.Domain == request.Domain && x.Id != id))
        {
            return Conflict(new { Message = "Домен организации уже существует." });
        }

        organization.Name = request.Name;
        organization.Domain = request.Domain;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id)
    {
        // IgnoreQueryFilters — чтобы найти запись даже если она уже помечена IsDeleted.
        // Без этого повторный DELETE вернул бы 404 вместо идемпотентного 204.
        var organization = await _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (organization == null)
        {
            return NotFound();
        }

        if (organization.Id != User.GetOrganizationId())
        {
            return Forbid();
        }

        organization.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
