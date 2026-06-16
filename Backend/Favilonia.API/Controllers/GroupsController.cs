using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Groups;
using Favilonia.Domain.Entities;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/groups")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class GroupsController : ControllerBase
{
    private readonly AppDbContext _db;

    public GroupsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupResponse>>> GetAll(Guid organizationId)
    {
        var groups = await _db.Groups
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new GroupResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Name = x.Name,
                StudentCount = x.Students.Count(s => s.GroupId == x.Id),
                CreatedAt = x.CreatedAt
            })
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GroupResponse>> GetById(Guid organizationId, Guid id)
    {
        var group = await _db.Groups
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new GroupResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Name = x.Name,
                StudentCount = x.Students.Count(s => s.GroupId == x.Id),
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (group is null)
            return NotFound();

        return Ok(group);
    }

    [HttpGet("{id}/students")]
    public async Task<ActionResult<IEnumerable<GroupStudentResponse>>> GetStudents(Guid organizationId, Guid id)
    {
        if (!await _db.Groups.AnyAsync(x => x.OrganizationId == organizationId && x.Id == id))
            return NotFound();

        var students = await _db.Users
            .Where(x => x.OrganizationId == organizationId && x.GroupId == id)
            .OrderBy(x => x.FullName)
            .Select(x => new GroupStudentResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email
            })
            .ToListAsync();

        return Ok(students);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<GroupResponse>> Create(Guid organizationId, CreateGroupRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
            return NotFound(new { Message = "Организация не найдена." });

        var group = new Group
        {
            OrganizationId = organizationId,
            Name = request.Name
        };

        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { organizationId, id = group.Id }, new GroupResponse
        {
            Id = group.Id,
            OrganizationId = group.OrganizationId,
            Name = group.Name,
            StudentCount = 0,
            CreatedAt = group.CreatedAt
        });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdateGroupRequest request)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (group is null)
            return NotFound();

        group.Name = request.Name;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Добавить студента в группу (убирает его из предыдущей, если была)
    [HttpPost("{id}/students/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> AddStudent(Guid organizationId, Guid id, Guid userId)
    {
        if (!await _db.Groups.AnyAsync(x => x.OrganizationId == organizationId && x.Id == id))
            return NotFound(new { Message = "Группа не найдена." });

        var user = await _db.Users.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == userId);
        if (user is null)
            return NotFound(new { Message = "Пользователь не найден." });

        user.GroupId = id;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Убрать студента из группы
    [HttpDelete("{id}/students/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> RemoveStudent(Guid organizationId, Guid id, Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == userId && x.GroupId == id);
        if (user is null)
            return NotFound();

        user.GroupId = null;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        // IgnoreQueryFilters — нужен, чтобы найти уже удалённую запись.
        var group = await _db.Groups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (group is null)
            return NotFound();

        group.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
