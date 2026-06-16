using Favilonia.API.Authorization;
using Favilonia.API.Dtos.Attendance;
using Favilonia.API.Dtos.Common;
using Favilonia.API.Extensions;
using Favilonia.Domain.Entities;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Favilonia.API.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/attendance")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _db;

    public AttendanceController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<AttendanceResponse>>> GetAll(
        Guid organizationId,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Студент не может смотреть посещаемость других — принудительно фильтруем по его ID.
        if (User.IsInRole(Roles.User))
            studentId = User.GetUserId();

        var query = _db.Attendances.Where(x => x.OrganizationId == organizationId);

        if (studentId.HasValue)
            query = query.Where(x => x.StudentId == studentId.Value);
        if (subjectId.HasValue)
            query = query.Where(x => x.SubjectId == subjectId.Value);
        if (from.HasValue)
            query = query.Where(x => x.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.Date <= to.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AttendanceResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                StudentId = x.StudentId,
                StudentName = x.Student.FullName,
                TeacherId = x.TeacherId,
                TeacherName = x.Teacher.FullName,
                SubjectId = x.SubjectId,
                SubjectName = x.Subject.Name,
                Date = x.Date,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new PaginationResponse<AttendanceResponse>(items, totalCount, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AttendanceResponse>> GetById(Guid organizationId, Guid id)
    {
        var record = await _db.Attendances
            .Where(x => x.OrganizationId == organizationId && x.Id == id)
            .Select(x => new AttendanceResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                StudentId = x.StudentId,
                StudentName = x.Student.FullName,
                TeacherId = x.TeacherId,
                TeacherName = x.Teacher.FullName,
                SubjectId = x.SubjectId,
                SubjectName = x.Subject.Name,
                Date = x.Date,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (record is null)
            return NotFound();

        // Студент не может смотреть чужую посещаемость.
        if (User.IsInRole(Roles.User) && record.StudentId != User.GetUserId())
            return Forbid();

        return Ok(record);
    }

    // Массовое проставление посещаемости для всей группы за один запрос.
    // Уже существующие записи (StudentId+SubjectId+Date) пропускаются — не перезаписываются.
    [HttpPost("bulk")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<BulkAttendanceResponse>> CreateBulk(Guid organizationId, BulkCreateAttendanceRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
            return NotFound(new { Message = "Организация не найдена." });

        if (!await _db.Users.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.TeacherId))
            return BadRequest(new { Message = "Учитель не найден в данной организации." });

        if (!await _db.Subjects.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.SubjectId))
            return BadRequest(new { Message = "Предмет не найден в данной организации." });

        var date = request.Date.Date;

        var existingStudentIds = await _db.Attendances
            .Where(x => x.SubjectId == request.SubjectId && x.Date == date
                        && request.Entries.Select(e => e.StudentId).Contains(x.StudentId))
            .Select(x => x.StudentId)
            .ToListAsync();

        var existingSet = existingStudentIds.ToHashSet();

        var newRecords = new List<Attendance>();
        foreach (var entry in request.Entries)
        {
            if (existingSet.Contains(entry.StudentId))
                continue;

            newRecords.Add(new Attendance
            {
                OrganizationId = organizationId,
                StudentId = entry.StudentId,
                TeacherId = request.TeacherId,
                SubjectId = request.SubjectId,
                Date = date,
                Status = entry.Status
            });
        }

        _db.Attendances.AddRange(newRecords);
        await _db.SaveChangesAsync();

        var items = await _db.Attendances
            .Where(x => newRecords.Select(r => r.Id).Contains(x.Id))
            .Select(x => new AttendanceResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                StudentId = x.StudentId,
                StudentName = x.Student.FullName,
                TeacherId = x.TeacherId,
                TeacherName = x.Teacher.FullName,
                SubjectId = x.SubjectId,
                SubjectName = x.Subject.Name,
                Date = x.Date,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new BulkAttendanceResponse
        {
            Created = newRecords.Count,
            Skipped = existingSet.Count,
            Items = items
        });
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<AttendanceResponse>> Create(Guid organizationId, CreateAttendanceRequest request)
    {
        if (!await _db.Organizations.AnyAsync(x => x.Id == organizationId))
            return NotFound(new { Message = "Организация не найдена." });

        if (!await _db.Users.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.StudentId))
            return BadRequest(new { Message = "Студент не найден в данной организации." });

        if (!await _db.Users.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.TeacherId))
            return BadRequest(new { Message = "Учитель не найден в данной организации." });

        if (!await _db.Subjects.AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.SubjectId))
            return BadRequest(new { Message = "Предмет не найден в данной организации." });

        // Нормализуем дату до начала дня UTC — время не имеет значения для посещаемости.
        var date = request.Date.Date;

        if (await _db.Attendances.AnyAsync(x => x.StudentId == request.StudentId
                && x.SubjectId == request.SubjectId
                && x.Date == date))
        {
            return Conflict(new { Message = "Запись о посещаемости для этого студента, предмета и даты уже существует." });
        }

        var attendance = new Attendance
        {
            OrganizationId = organizationId,
            StudentId = request.StudentId,
            TeacherId = request.TeacherId,
            SubjectId = request.SubjectId,
            Date = date,
            Status = request.Status
        };

        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync();

        var response = new AttendanceResponse
        {
            Id = attendance.Id,
            OrganizationId = attendance.OrganizationId,
            StudentId = attendance.StudentId,
            StudentName = (await _db.Users.FindAsync(attendance.StudentId))?.FullName ?? string.Empty,
            TeacherId = attendance.TeacherId,
            TeacherName = (await _db.Users.FindAsync(attendance.TeacherId))?.FullName ?? string.Empty,
            SubjectId = attendance.SubjectId,
            SubjectName = (await _db.Subjects.FindAsync(attendance.SubjectId))?.Name ?? string.Empty,
            Date = attendance.Date,
            Status = attendance.Status,
            CreatedAt = attendance.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { organizationId, id = attendance.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, UpdateAttendanceRequest request)
    {
        var attendance = await _db.Attendances.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (attendance is null)
            return NotFound();

        attendance.Status = request.Status;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        var attendance = await _db.Attendances.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id);
        if (attendance is null)
            return NotFound();

        _db.Attendances.Remove(attendance);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
