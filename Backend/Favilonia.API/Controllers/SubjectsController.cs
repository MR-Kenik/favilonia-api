using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Subjects;
using Favilonia.Domain.Entities;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/subjects")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class SubjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubjectsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubjectResponse>>> GetAll(Guid organizationId)
    {
        var subjects = await _db.Subjects
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.Name)
            .Select(x => new SubjectResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Name = x.Name,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(subjects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectResponse>> GetById(Guid organizationId, Guid id)
    {
        var subject = await _db.Subjects
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new SubjectResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Name = x.Name,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (subject is null)
            return NotFound();

        return Ok(subject);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<SubjectResponse>> Create(Guid organizationId, CreateSubjectRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
            return NotFound(new { Message = "Организация не найдена." });

        var subject = new Subject
        {
            OrganizationId = organizationId,
            Name = request.Name
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        var response = new SubjectResponse
        {
            Id = subject.Id,
            OrganizationId = subject.OrganizationId,
            Name = subject.Name,
            CreatedAt = subject.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = subject.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdateSubjectRequest request)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (subject is null)
            return NotFound();

        subject.Name = request.Name;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        // IgnoreQueryFilters — нужен, чтобы найти уже удалённую запись (глобальный фильтр IsDeleted её скрывает).
        var subject = await _db.Subjects.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (subject is null)
            return NotFound();

        subject.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
