using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Common;
using Favilonia.API.Dtos.News;
using Favilonia.Infrastructure.Data;
using Favilonia.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/[controller]")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class NewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NewsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<NewsResponse>>> GetAll(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Валидация параметров пагинации
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // максимум 100 элементов на странице

        // Подсчитываем всего элементов для этой организации
        var totalCount = await _db.News
            .Where(x => x.OrganizationId == organizationId)
            .CountAsync();

        // Получаем элементы текущей страницы
        var news = await _db.News
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
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

        var response = new PaginationResponse<NewsResponse>(news, totalCount, page, pageSize);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NewsResponse>> GetById(Guid organizationId, Guid id)
    {
        var item = await _db.News
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new NewsResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                Title = x.Title,
                Content = x.Content,
                PublishedAt = x.PublishedAt,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<NewsResponse>> Create(Guid organizationId, CreateNewsRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
        {
            return NotFound(new { Message = "Организация не найдена." });
        }

        var news = new News
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = request.Title,
            Content = request.Content,
            PublishedAt = request.PublishedAt
        };

        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var response = new NewsResponse
        {
            Id = news.Id,
            OrganizationId = news.OrganizationId,
            Title = news.Title,
            Content = news.Content,
            PublishedAt = news.PublishedAt,
            CreatedAt = news.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = news.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdateNewsRequest request)
    {
        var news = await _db.News.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (news == null)
        {
            return NotFound();
        }

        news.Title = request.Title;
        news.Content = request.Content;
        news.PublishedAt = request.PublishedAt;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        // IgnoreQueryFilters — нужен, чтобы найти уже удалённую запись (глобальный фильтр IsDeleted её скрывает).
        var news = await _db.News.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (news == null)
        {
            return NotFound();
        }

        news.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
