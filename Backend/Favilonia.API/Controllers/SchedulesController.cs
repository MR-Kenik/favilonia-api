using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Common;
using Favilonia.API.Dtos.Schedules;
using Favilonia.Infrastructure.Data;
using Favilonia.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/[controller]")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SchedulesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<ScheduleResponse>>> GetAll(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Валидация параметров пагинации
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // максимум 100 элементов на странице

        // Подсчитываем всего элементов для этой организации
        var totalCount = await _db.Schedules
            .Where(x => x.OrganizationId == organizationId)
            .CountAsync();

        // Получаем элементы текущей страницы
        var schedules = await _db.Schedules
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ScheduleResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Title = x.Title,
                Description = x.Description,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        var response = new PaginationResponse<ScheduleResponse>(schedules, totalCount, page, pageSize);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduleResponse>> GetById(Guid organizationId, Guid id)
    {
        var schedule = await _db.Schedules
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new ScheduleResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Title = x.Title,
                Description = x.Description,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (schedule == null)
        {
            return NotFound();
        }

        return Ok(schedule);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<ScheduleResponse>> Create(Guid organizationId, CreateScheduleRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
        {
            return NotFound(new { Message = "Организация не найдена." });
        }

        if (request.StartDate >= request.EndDate)
        {
            return BadRequest(new { Message = "Дата начала должна быть меньше даты окончания." });
        }

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync();

        var response = new ScheduleResponse
        {
            Id = schedule.Id,
            OrganizationId = schedule.OrganizationId,
            Title = schedule.Title,
            Description = schedule.Description,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            CreatedAt = schedule.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = schedule.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]   
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdateScheduleRequest request)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (schedule == null)
        {
            return NotFound();
        }

        if (request.StartDate >= request.EndDate)
        {
            return BadRequest(new { Message = "Дата начала должна быть меньше даты окончания." });
        }

        schedule.Title = request.Title;
        schedule.Description = request.Description;
        schedule.StartDate = request.StartDate;
        schedule.EndDate = request.EndDate;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        // IgnoreQueryFilters — нужен, чтобы найти уже удалённую запись (глобальный фильтр IsDeleted её скрывает).
        var schedule = await _db.Schedules.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (schedule == null)
        {
            return NotFound();
        }

        schedule.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
