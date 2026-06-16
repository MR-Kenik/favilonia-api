using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Common;
using Favilonia.API.Dtos.Feedbacks;
using Favilonia.Infrastructure.Data;
using Favilonia.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/[controller]")]
public class FeedbacksController : ControllerBase
{
    private readonly AppDbContext _db;

    public FeedbacksController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Отправить обращение/обратную связь (доступно без авторизации)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<FeedbackResponse>> Create(Guid organizationId, CreateFeedbackRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
        {
            return NotFound(new { Message = "Организация не найдена." });
        }

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name,
            Email = request.Email,
            Message = request.Message,
            IsRead = false
        };

        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        var response = new FeedbackResponse
        {
            Id = feedback.Id,
            OrganizationId = feedback.OrganizationId,
            Name = feedback.Name,
            Email = feedback.Email,
            Message = feedback.Message,
            IsRead = feedback.IsRead,
            CreatedAt = feedback.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = feedback.Id }, response);
    }

    /// <summary>
    /// Получить все обращения организации (только для администраторов)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SameOrganization)]
    public async Task<ActionResult<PaginationResponse<FeedbackResponse>>> GetAll(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var totalCount = await _db.Feedbacks
            .Where(x => x.OrganizationId == organizationId)
            .CountAsync();

        var feedbacks = await _db.Feedbacks
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FeedbackResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Name = x.Name,
                Email = x.Email,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        var response = new PaginationResponse<FeedbackResponse>(feedbacks, totalCount, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Получить одно обращение (только для администраторов)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.SameOrganization)]
    public async Task<ActionResult<FeedbackResponse>> GetById(Guid organizationId, Guid id)
    {
        var feedback = await _db.Feedbacks
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new FeedbackResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Name = x.Name,
                Email = x.Email,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (feedback == null)
        {
            return NotFound();
        }

        return Ok(feedback);
    }

    /// <summary>
    /// Отметить обращение как прочитанное (только для администраторов)
    /// </summary>
    [HttpPost("{id}/mark-as-read")]
    [Authorize(Policy = AuthorizationPolicies.SameOrganization)]
    public async Task<IActionResult> MarkAsRead(Guid organizationId, Guid id)
    {
        var feedback = await _db.Feedbacks.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (feedback == null)
        {
            return NotFound();
        }

        feedback.IsRead = true;
        feedback.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Удалить обращение (только для администраторов)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.SameOrganization)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        var feedback = await _db.Feedbacks.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (feedback == null)
        {
            return NotFound();
        }

        _db.Feedbacks.Remove(feedback);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
