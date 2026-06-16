using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Common;
using Favilonia.API.Dtos.Pages;
using Favilonia.Infrastructure.Data;
using Favilonia.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/[controller]")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class PagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PagesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<PageResponse>> GetBySlug(Guid organizationId, string slug)
    {
        var page = await _db.Pages
            .Where(x => x.OrganizationId == organizationId && x.Slug == slug)
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

        if (page == null)
        {
            return NotFound("Страница не найдена.");
        }

        return Ok(page);
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<PageResponse>>> GetAll(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Валидация параметров пагинации
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Подсчитываем всего элементов для этой организации
        var totalCount = await _db.Pages
            .Where(x => x.OrganizationId == organizationId)
            .CountAsync();

        // Получаем элементы текущей страницы
        var pages = await _db.Pages
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync();

        var response = new PaginationResponse<PageResponse>(pages, totalCount, page, pageSize);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PageResponse>> GetById(Guid organizationId, Guid id)
    {
        var page = await _db.Pages
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
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

        if (page == null)
        {
            return NotFound();
        }

        return Ok(page);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<PageResponse>> Create(Guid organizationId, CreatePageRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
        {
            return NotFound(new { Message = "Организация не найдена." });
        }

        if (await _db.Pages.AnyAsync(x => x.OrganizationId == organizationId && x.Slug == request.Slug))
        {
            return Conflict(new { Message = "Страница с таким slug уже существует." });
        }

        var page = new Page
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = request.Title,
            Slug = request.Slug,
            Content = request.Content,
            IsPublished = request.IsPublished
        };

        _db.Pages.Add(page);
        await _db.SaveChangesAsync();

        var response = new PageResponse
        {
            Id = page.Id,
            OrganizationId = page.OrganizationId,
            Title = page.Title,
            Slug = page.Slug,
            Content = page.Content,
            IsPublished = page.IsPublished,
            CreatedAt = page.CreatedAt,
            UpdatedAt = page.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = page.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdatePageRequest request)
    {
        var page = await _db.Pages.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (page == null)
        {
            return NotFound();
        }

        if (await _db.Pages.AnyAsync(x => x.OrganizationId == organizationId && x.Slug == request.Slug && x.Id != id))
        {
            return Conflict(new { Message = "Страница с таким slug уже существует." });
        }

        page.Title = request.Title;
        page.Slug = request.Slug;
        page.Content = request.Content;
        page.IsPublished = request.IsPublished;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        // IgnoreQueryFilters — нужен, чтобы найти уже удалённую запись (глобальный фильтр IsDeleted её скрывает).
        var page = await _db.Pages.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (page == null)
        {
            return NotFound();
        }

        page.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
