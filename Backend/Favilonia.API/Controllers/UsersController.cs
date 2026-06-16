using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Users;
using Favilonia.API.Extensions;
using Favilonia.API.Services;
using Favilonia.Infrastructure.Data;
using Favilonia.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/[controller]")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(Guid organizationId)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var user = await _db.Users
            .Where(x => x.OrganizationId == organizationId && x.Id == userId)
            .Select(x => new UserResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Email = x.Email,
                FullName = x.FullName,
                Role = x.Role,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(Guid organizationId, UpdateMeRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == userId);
        if (user is null)
            return NotFound();

        user.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Список пользователей — только для Admin.
    // Студенты (роль User) не должны видеть друг друга; для своего профиля используй GET /me.
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(Guid organizationId, [FromQuery] string? search = null)
    {
        var query = _db.Users.Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(term) || x.Email.ToLower().Contains(term));
        }

        var users = await query
            .OrderBy(x => x.FullName)
            .Select(x => new UserResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Email = x.Email,
                FullName = x.FullName,
                Role = x.Role,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid organizationId, Guid id)
    {
        var user = await _db.Users
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new UserResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Email = x.Email,
                FullName = x.FullName,
                Role = x.Role,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Create(Guid organizationId, CreateUserRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
        {
            return NotFound(new { Message = "Организация не найдена." });
        }

        // Первый пользователь в организации создаётся анонимно (онбординг).
        // Все последующие — только администратором, иначе любой смог бы добавлять себя в чужую организацию.
        var existingUsers = await _db.Users.CountAsync(x => x.OrganizationId == organizationId);
        if (existingUsers > 0)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }

            if (!User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }
        }

        if (await _db.Users.AnyAsync(x => x.OrganizationId == organizationId && x.Email == request.Email))
        {
            return Conflict(new { Message = "Пользователь с таким адресом электронной почты уже существует в организации." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            FullName = request.FullName,
            Role = request.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var response = new UserResponse
        {
            Id = user.Id,
            OrganizationId = user.OrganizationId,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = user.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdateUserRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = PasswordHasher.Hash(request.Password);
        }

        user.FullName = request.FullName;
        user.Role = request.Role;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        // Пользователи удаляются физически (в отличие от новостей/страниц — нет IsDeleted).
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Экспорт студентов в CSV (открывается в Excel без лишних настроек).
    // groupId — опционально: экспортировать только студентов конкретной группы.
    [HttpGet("export")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Export(Guid organizationId, [FromQuery] Guid? groupId = null)
    {
        var query = _db.Users
            .Where(x => x.OrganizationId == organizationId && x.Role == Roles.User);

        if (groupId.HasValue)
            query = query.Where(x => x.GroupId == groupId.Value);

        var users = await query
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Email,
                x.Role,
                GroupName = x.Group != null ? x.Group.Name : string.Empty,
                x.CreatedAt
            })
            .ToListAsync();

        // BOM (﻿) нужен чтобы Excel автоматически определил кодировку UTF-8 и не показывал кракозябры.
        var sb = new System.Text.StringBuilder();
        sb.Append('﻿');
        sb.AppendLine("Id;ФИО;Email;Роль;Группа;Дата регистрации");

        foreach (var u in users)
            sb.AppendLine($"{u.Id};{EscapeCsv(u.FullName)};{EscapeCsv(u.Email)};{u.Role};{EscapeCsv(u.GroupName)};{u.CreatedAt:dd.MM.yyyy HH:mm}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", "students.csv");
    }

    // Сверка списка из CSV с базой: кто уже зарегистрирован, кого ещё нет.
    // Ожидаемый формат CSV (разделитель ; или ,, первая строка — заголовок):
    //   ФИО;Email
    //   Иванов Иван;ivanov@school.ru
    [HttpPost("import/check")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<ImportCheckResponse>> ImportCheck(
        Guid organizationId,
        IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "Файл не передан или пуст." });

        string content;
        using (var reader = new System.IO.StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8))
            content = await reader.ReadToEndAsync();

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
            return BadRequest(new { Message = "CSV должен содержать заголовок и хотя бы одну строку данных." });

        // Определяем разделитель по первой строке (заголовку).
        var header = lines[0];
        var delimiter = header.Contains(';') ? ';' : ',';

        // Парсим строки, пропуская заголовок.
        var fileEntries = new List<(string FullName, string Email)>();
        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(delimiter);
            if (parts.Length < 2) continue;

            var fullName = parts[0].Trim().Trim('"');
            var email = parts[1].Trim().Trim('"').ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email)) continue;
            fileEntries.Add((fullName, email));
        }

        if (fileEntries.Count == 0)
            return BadRequest(new { Message = "Не удалось прочитать ни одной строки из файла. Проверьте формат CSV." });

        // Загружаем всех студентов организации одним запросом и матчим в памяти.
        var emailsInFile = fileEntries.Select(e => e.Email).ToHashSet();

        var existingUsers = await _db.Users
            .Where(x => x.OrganizationId == organizationId && emailsInFile.Contains(x.Email.ToLower()))
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Email,
                GroupName = x.Group != null ? x.Group.Name : null
            })
            .ToListAsync();

        var existingByEmail = existingUsers.ToDictionary(u => u.Email.ToLowerInvariant());

        var registered = new List<ImportCheckRegisteredItem>();
        var unregistered = new List<ImportCheckUnregisteredItem>();

        foreach (var entry in fileEntries)
        {
            if (existingByEmail.TryGetValue(entry.Email, out var found))
            {
                registered.Add(new ImportCheckRegisteredItem
                {
                    UserId = found.Id,
                    FullName = found.FullName,
                    Email = found.Email,
                    GroupName = found.GroupName
                });
            }
            else
            {
                unregistered.Add(new ImportCheckUnregisteredItem
                {
                    FullName = entry.FullName,
                    Email = entry.Email
                });
            }
        }

        return Ok(new ImportCheckResponse
        {
            TotalInFile = fileEntries.Count,
            RegisteredCount = registered.Count,
            UnregisteredCount = unregistered.Count,
            Registered = registered,
            Unregistered = unregistered
        });
    }

    // Экранирование значения CSV: если содержит разделитель или кавычку — оборачиваем в кавычки.
    private static string EscapeCsv(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
