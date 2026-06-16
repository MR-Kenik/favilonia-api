using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Common;
using Favilonia.API.Dtos.FinalGrades;
using Favilonia.API.Extensions;
using Favilonia.Domain.Entities;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/final-grades")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class FinalGradesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FinalGradesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<FinalGradeResponse>>> GetAll(
        Guid organizationId,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? periodId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Студент не может смотреть итоговые оценки других.
        if (User.IsInRole(Roles.User))
            studentId = User.GetUserId();

        var query = _db.FinalGrades.Where(x => x.OrganizationId == organizationId);

        if (studentId.HasValue)
            query = query.Where(x => x.StudentId == studentId.Value);
        if (subjectId.HasValue)
            query = query.Where(x => x.SubjectId == subjectId.Value);
        if (periodId.HasValue)
            query = query.Where(x => x.PeriodId == periodId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FinalGradeResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                StudentId = x.StudentId,
                StudentName = x.Student.FullName,
                TeacherId = x.TeacherId,
                TeacherName = x.Teacher.FullName,
                SubjectId = x.SubjectId,
                SubjectName = x.Subject.Name,
                PeriodId = x.PeriodId,
                PeriodName = x.Period.Name,
                Value = x.Value,
                Comment = x.Comment,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new PaginationResponse<FinalGradeResponse>(items, totalCount, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FinalGradeResponse>> GetById(Guid organizationId, Guid id)
    {
        var finalGrade = await _db.FinalGrades
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new FinalGradeResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                StudentId = x.StudentId,
                StudentName = x.Student.FullName,
                TeacherId = x.TeacherId,
                TeacherName = x.Teacher.FullName,
                SubjectId = x.SubjectId,
                SubjectName = x.Subject.Name,
                PeriodId = x.PeriodId,
                PeriodName = x.Period.Name,
                Value = x.Value,
                Comment = x.Comment,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (finalGrade is null)
            return NotFound();

        // Студент не может смотреть чужие итоговые оценки.
        if (User.IsInRole(Roles.User) && finalGrade.StudentId != User.GetUserId())
            return Forbid();

        return Ok(finalGrade);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<FinalGradeResponse>> Create(Guid organizationId, CreateFinalGradeRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
            return NotFound(new { Message = "Организация не найдена." });

        if (!await _db.Users.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.StudentId))
            return BadRequest(new { Message = "Студент не найден в данной организации." });

        if (!await _db.Users.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.TeacherId))
            return BadRequest(new { Message = "Учитель не найден в данной организации." });

        if (!await _db.Subjects.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.SubjectId))
            return BadRequest(new { Message = "Предмет не найден в данной организации." });

        if (!await _db.Periods.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.PeriodId))
            return BadRequest(new { Message = "Период не найден в данной организации." });

        // Уникальная итоговая оценка на студента/предмет/период.
        var exists = await _db.FinalGrades.AnyAsync(x =>
            x.StudentId == request.StudentId &&
            x.SubjectId == request.SubjectId &&
            x.PeriodId == request.PeriodId);

        if (exists)
            return Conflict(new { Message = "Итоговая оценка для этого студента, предмета и периода уже существует." });

        var finalGrade = new FinalGrade
        {
            OrganizationId = organizationId,
            StudentId = request.StudentId,
            TeacherId = request.TeacherId,
            SubjectId = request.SubjectId,
            PeriodId = request.PeriodId,
            Value = request.Value,
            Comment = request.Comment
        };

        _db.FinalGrades.Add(finalGrade);
        await _db.SaveChangesAsync();

        // Загружаем связанные имена для ответа.
        var student = await _db.Users.FindAsync(finalGrade.StudentId);
        var teacher = await _db.Users.FindAsync(finalGrade.TeacherId);
        var subject = await _db.Subjects.FindAsync(finalGrade.SubjectId);
        var period = await _db.Periods.FindAsync(finalGrade.PeriodId);

        var response = new FinalGradeResponse
        {
            Id = finalGrade.Id,
            OrganizationId = finalGrade.OrganizationId,
            StudentId = finalGrade.StudentId,
            StudentName = student?.FullName ?? string.Empty,
            TeacherId = finalGrade.TeacherId,
            TeacherName = teacher?.FullName ?? string.Empty,
            SubjectId = finalGrade.SubjectId,
            SubjectName = subject?.Name ?? string.Empty,
            PeriodId = finalGrade.PeriodId,
            PeriodName = period?.Name ?? string.Empty,
            Value = finalGrade.Value,
            Comment = finalGrade.Comment,
            CreatedAt = finalGrade.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = finalGrade.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdateFinalGradeRequest request)
    {
        var finalGrade = await _db.FinalGrades.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (finalGrade is null)
            return NotFound();

        finalGrade.Value = request.Value;
        finalGrade.Comment = request.Comment;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        var finalGrade = await _db.FinalGrades.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (finalGrade is null)
            return NotFound();

        _db.FinalGrades.Remove(finalGrade);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
