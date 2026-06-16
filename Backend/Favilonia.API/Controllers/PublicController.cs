using Favilonia.API.Dtos.Common;
using Favilonia.API.Dtos.News;
using Favilonia.API.Dtos.Pages;
using Favilonia.API.Dtos.Public;
using Favilonia.API.Dtos.Schedules;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

// Публичный анонимный слой чтения — без авторизации.
// Резолвит организацию по домену (не по GUID), отдаёт только опубликованный контент.
// Черновики новостей (PublishedAt == null) и неопубликованные страницы (IsPublished = false) сюда не попадают.
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _db;

    public PublicController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{domain}")]
    public async Task<ActionResult<OrganizationInfoResponse>> GetOrganization(string domain)
    {
        var org = await _db.Organizations
            .Where(x => x.Domain == domain)
            .Select(x => new OrganizationInfoResponse { Id = x.Id, Name = x.Name })
            .FirstOrDefaultAsync();

        if (org is null)
            return NotFound();

        return Ok(org);
    }

    [HttpGet("{domain}/news")]
    public async Task<ActionResult<PaginationResponse<NewsResponse>>> GetNews(
        string domain, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(x => x.Domain == domain);
        if (org is null)
            return NotFound();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.News.Where(x => x.OrganizationId == org.Id && x.PublishedAt != null);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NewsResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Title = x.Title,
                Content = x.Content,
                PublishedAt = x.PublishedAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new PaginationResponse<NewsResponse>(items, totalCount, page, pageSize));
    }

    [HttpGet("{domain}/schedule")]
    public async Task<ActionResult<PaginationResponse<ScheduleResponse>>> GetSchedule(
        string domain, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(x => x.Domain == domain);
        if (org is null)
            return NotFound();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Показываем только актуальное расписание: события, которые ещё не закончились.
        var now = DateTime.UtcNow;
        var query = _db.Schedules.Where(x => x.OrganizationId == org.Id && x.EndDate >= now);

        var totalCount = await query.CountAsync();

        var items = await query
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

        return Ok(new PaginationResponse<ScheduleResponse>(items, totalCount, page, pageSize));
    }

    [HttpGet("{domain}/pages")]
    public async Task<ActionResult<List<PublicPageListItemResponse>>> GetPages(string domain)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(x => x.Domain == domain);
        if (org is null)
            return NotFound();

        var items = await _db.Pages
            .Where(x => x.OrganizationId == org.Id && x.IsPublished)
            .OrderBy(x => x.Title)
            .Select(x => new PublicPageListItemResponse { Title = x.Title, Slug = x.Slug })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{domain}/pages/{slug}")]
    public async Task<ActionResult<PageResponse>> GetPageBySlug(string domain, string slug)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(x => x.Domain == domain);
        if (org is null)
            return NotFound();

        var page = await _db.Pages
            .Where(x => x.OrganizationId == org.Id && x.Slug == slug && x.IsPublished)
            .Select(x => new PageResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Title = x.Title,
                Slug = x.Slug,
                Content = x.Content,
                IsPublished = x.IsPublished,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (page is null)
            return NotFound();

        return Ok(page);
    }
}
