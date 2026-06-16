using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Periods;
using Favilonia.Domain.Entities;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/periods")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class PeriodsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PeriodsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PeriodResponse>>> GetAll(Guid organizationId)
    {
        var periods = await _db.Periods
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.StartDate)
            .Select(x => new PeriodResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Name = x.Name,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(periods);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PeriodResponse>> GetById(Guid organizationId, Guid id)
    {
        var period = await _db.Periods
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new PeriodResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Name = x.Name,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (period is null)
            return NotFound();

        return Ok(period);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<PeriodResponse>> Create(Guid organizationId, CreatePeriodRequest request)
    {
        if (request.EndDate <= request.StartDate)
            return BadRequest(new { Message = "Дата окончания должна быть позже даты начала." });

        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
            return NotFound(new { Message = "Организация не найдена." });

        var period = new Period
        {
            OrganizationId = organizationId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _db.Periods.Add(period);
        await _db.SaveChangesAsync();

        var response = new PeriodResponse
        {
            Id = period.Id,
            OrganizationId = period.OrganizationId,
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            CreatedAt = period.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = period.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdatePeriodRequest request)
    {
        if (request.EndDate <= request.StartDate)
            return BadRequest(new { Message = "Дата окончания должна быть позже даты начала." });

        var period = await _db.Periods.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (period is null)
            return NotFound();

        period.Name = request.Name;
        period.StartDate = request.StartDate;
        period.EndDate = request.EndDate;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        // IgnoreQueryFilters — нужен чтобы найти уже удалённую запись (глобальный фильтр IsDeleted её скрывает).
        var period = await _db.Periods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (period is null)
            return NotFound();

        period.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
