using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Common;
using Favilonia.API.Dtos.Grades;
using Favilonia.API.Extensions;
using Favilonia.Domain.Entities;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/grades")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class GradesController : ControllerBase
{
    private readonly AppDbContext _db;

    public GradesController(AppDbContext db)
    {
        _db = db;
    }

    // Сводная статистика по студенту+предмету: средний балл и процент посещаемости.
    // Студент видит только свою статистику; администратор может запросить любую.
    // periodId — опциональный фильтр: считать только оценки конкретного периода.
    [HttpGet("summary")]
    public async Task<ActionResult<GradeSummaryResponse>> GetSummary(
        Guid organizationId,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? periodId = null)
    {
        if (User.IsInRole(Roles.User))
            studentId = User.GetUserId();

        if (!studentId.HasValue || !subjectId.HasValue)
            return BadRequest(new { Message = "Параметры studentId и subjectId обязательны." });

        var student = await _db.Users
            .Where(x => x.OrganizationId == organizationId && x.Id == studentId.Value)
            .Select(x => new { x.Id, x.FullName })
            .FirstOrDefaultAsync();

        if (student is null)
            return NotFound(new { Message = "Студент не найден." });

        var subject = await _db.Subjects
            .Where(x => x.OrganizationId == organizationId && x.Id == subjectId.Value)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync();

        if (subject is null)
            return NotFound(new { Message = "Предмет не найден." });

        var gradesQuery = _db.Grades
            .Where(x => x.OrganizationId == organizationId
                        && x.StudentId == studentId.Value
                        && x.SubjectId == subjectId.Value);

        if (periodId.HasValue)
            gradesQuery = gradesQuery.Where(x => x.PeriodId == periodId.Value);

        var grades = await gradesQuery.Select(x => x.Value).ToListAsync();

        var attendance = await _db.Attendances
            .Where(x => x.OrganizationId == organizationId
                        && x.StudentId == studentId.Value
                        && x.SubjectId == subjectId.Value)
            .Select(x => x.Status)
            .ToListAsync();

        // Present и Late считаются присутствием
        var presentCount = attendance.Count(s => s == AttendanceStatus.Present || s == AttendanceStatus.Late);
        var attendancePct = attendance.Count > 0
            ? Math.Round((double)presentCount / attendance.Count * 100, 1)
            : 0;

        return Ok(new GradeSummaryResponse
        {
            StudentId = student.Id,
            StudentName = student.FullName,
            SubjectId = subject.Id,
            SubjectName = subject.Name,
            AverageGrade = grades.Count > 0 ? Math.Round(grades.Average(), 2) : 0,
            GradeCount = grades.Count,
            AttendanceTotal = attendance.Count,
            AttendancePresent = presentCount,
            AttendancePercent = attendancePct
        });
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<GradeResponse>>> GetAll(
        Guid organizationId,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] Guid? periodId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Студент не может смотреть оценки других — принудительно фильтруем по его ID.
        if (User.IsInRole(Roles.User))
            studentId = User.GetUserId();

        var query = _db.Grades.Where(x => x.OrganizationId == organizationId);

        if (studentId.HasValue)
            query = query.Where(x => x.StudentId == studentId.Value);
        if (subjectId.HasValue)
            query = query.Where(x => x.SubjectId == subjectId.Value);
        if (periodId.HasValue)
            query = query.Where(x => x.PeriodId == periodId.Value);
        if (from.HasValue)
            query = query.Where(x => x.GradedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.GradedAt <= to.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.GradedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new GradeResponse
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
                PeriodName = x.Period != null ? x.Period.Name : null,
                Value = x.Value,
                GradeType = x.GradeType,
                Comment = x.Comment,
                GradedAt = x.GradedAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new PaginationResponse<GradeResponse>(items, totalCount, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GradeResponse>> GetById(Guid organizationId, Guid id)
    {
        var grade = await _db.Grades
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new GradeResponse
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
                PeriodName = x.Period != null ? x.Period.Name : null,
                Value = x.Value,
                GradeType = x.GradeType,
                Comment = x.Comment,
                GradedAt = x.GradedAt,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (grade is null)
            return NotFound();

        // Студент не может смотреть чужие оценки.
        if (User.IsInRole(Roles.User) && grade.StudentId != User.GetUserId())
            return Forbid();

        return Ok(grade);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<GradeResponse>> Create(Guid organizationId, CreateGradeRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
            return NotFound(new { Message = "Организация не найдена." });

        if (!await _db.Users.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.StudentId))
            return BadRequest(new { Message = "Студент не найден в данной организации." });

        if (!await _db.Users.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.TeacherId))
            return BadRequest(new { Message = "Учитель не найден в данной организации." });

        if (!await _db.Subjects.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.SubjectId))
            return BadRequest(new { Message = "Предмет не найден в данной организации." });

        var grade = new Grade
        {
            OrganizationId = organizationId,
            StudentId = request.StudentId,
            TeacherId = request.TeacherId,
            SubjectId = request.SubjectId,
            Value = request.Value,
            GradeType = request.GradeType,
            Comment = request.Comment,
            GradedAt = request.GradedAt
        };

        _db.Grades.Add(grade);
        await _db.SaveChangesAsync();

        var response = new GradeResponse
        {
            Id = grade.Id,
            OrganizationId = grade.OrganizationId,
            StudentId = grade.StudentId,
            StudentName = (await _db.Users.FindAsync(grade.StudentId))?.FullName ?? string.Empty,
            TeacherId = grade.TeacherId,
            TeacherName = (await _db.Users.FindAsync(grade.TeacherId))?.FullName ?? string.Empty,
            SubjectId = grade.SubjectId,
            SubjectName = (await _db.Subjects.FindAsync(grade.SubjectId))?.Name ?? string.Empty,
            Value = grade.Value,
            Comment = grade.Comment,
            GradedAt = grade.GradedAt,
            CreatedAt = grade.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = grade.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdateGradeRequest request)
    {
        var grade = await _db.Grades.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (grade is null)
            return NotFound();

        grade.Value = request.Value;
        grade.GradeType = request.GradeType;
        grade.Comment = request.Comment;
        grade.GradedAt = request.GradedAt;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        var grade = await _db.Grades.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (grade is null)
            return NotFound();

        _db.Grades.Remove(grade);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
