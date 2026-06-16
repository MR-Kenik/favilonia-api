using Favilonia.Domain.Entities;
using Favilonia.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

// AttendanceStatus и GradeType — статические классы с константами, лежат в Domain.Entities
using AttendanceStatus = Favilonia.Domain.Entities.AttendanceStatus;
using GradeType = Favilonia.Domain.Entities.GradeType;

namespace Favilonia.Infrastructure.Data;

/// <summary>
/// Начальное заполнение базы данных тестовыми данными
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // Проверяем, не инициализирована ли уже база
        if (await context.Organizations.AnyAsync())
        {
            return; // БД уже заполнена
        }

        // Создаём тестовую организацию
        var organization = new Organization
        {
            Id = new Guid("12345678-1234-1234-1234-123456789012"),
            Name = "Демо Школа",
            Domain = "demo-school",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        // Создаём администратора организации
        var admin = new User
        {
            Id = new Guid("87654321-4321-4321-4321-210987654321"),
            OrganizationId = organization.Id,
            Email = "admin@demo-school.local",
            PasswordHash = PasswordHasher.Hash("Admin@123456"),
            FullName = "Администратор",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        // Создаём демо-студентов
        var student1 = new User
        {
            Id = new Guid("11111111-1111-1111-1111-111111111111"),
            OrganizationId = organization.Id,
            Email = "ivanov@demo-school.local",
            PasswordHash = PasswordHasher.Hash("User@123456"),
            FullName = "Иванов Иван Иванович",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var student2 = new User
        {
            Id = new Guid("22222222-2222-2222-2222-222222222222"),
            OrganizationId = organization.Id,
            Email = "petrova@demo-school.local",
            PasswordHash = PasswordHasher.Hash("User@123456"),
            FullName = "Петрова Мария Сергеевна",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var student3 = new User
        {
            Id = new Guid("33333333-3333-3333-3333-333333333333"),
            OrganizationId = organization.Id,
            Email = "sidorov@demo-school.local",
            PasswordHash = PasswordHasher.Hash("User@123456"),
            FullName = "Сидоров Алексей Петрович",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(student1, student2, student3);
        await context.SaveChangesAsync();

        // Создаём новости
        var news = new[]
        {
            new News
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Title = "Добро пожаловать в Favilonia!",
                Content = "Это демонстрационная новость. Favilonia - SaaS-платформа для учебных заведений.",
                PublishedAt = DateTime.UtcNow,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new News
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Title = "Начало учебного года",
                Content = "Новый учебный год начинается с понедельника. Ожидаем всех учеников в школе.",
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        context.News.AddRange(news);
        await context.SaveChangesAsync();

        // Создаём расписание
        var now = DateTime.UtcNow;
        var schedule = new[]
        {
            new Schedule
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Title = "Учебные занятия",
                Description = "Основные учебные занятия для всех классов",
                StartDate = now.Date.AddHours(8),
                EndDate = now.Date.AddHours(16),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Schedule
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Title = "Дополнительные занятия",
                Description = "Факультативные и дополнительные занятия",
                StartDate = now.Date.AddDays(1).AddHours(16),
                EndDate = now.Date.AddDays(1).AddHours(18),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Schedules.AddRange(schedule);
        await context.SaveChangesAsync();

        // --- Электронный журнал ---

        // Группа
        var group = new Group
        {
            Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            OrganizationId = organization.Id,
            Name = "10А",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // Привязываем студентов к группе
        student1.GroupId = group.Id;
        student2.GroupId = group.Id;
        student3.GroupId = group.Id;
        await context.SaveChangesAsync();

        // Предметы
        var subjectMath = new Subject
        {
            Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            OrganizationId = organization.Id,
            Name = "Математика",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var subjectRussian = new Subject
        {
            Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            OrganizationId = organization.Id,
            Name = "Русский язык",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var subjectPhysics = new Subject
        {
            Id = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            OrganizationId = organization.Id,
            Name = "Физика",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Subjects.AddRange(subjectMath, subjectRussian, subjectPhysics);
        await context.SaveChangesAsync();

        // Учебные периоды
        var year = DateTime.UtcNow.Year;

        var period1 = new Period
        {
            Id = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            OrganizationId = organization.Id,
            Name = "I четверть",
            StartDate = new DateTime(year, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(year, 10, 31, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var period2 = new Period
        {
            Id = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            OrganizationId = organization.Id,
            Name = "II четверть",
            StartDate = new DateTime(year, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(year, 12, 25, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Periods.AddRange(period1, period2);
        await context.SaveChangesAsync();

        // Оценки (I четверть, Математика)
        var grades = new[]
        {
            new Grade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student1.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, PeriodId = period1.Id, Value = 5, GradeType = GradeType.ControlWork, Comment = "Отлично справился", GradedAt = period1.StartDate.AddDays(14), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Grade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student1.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, PeriodId = period1.Id, Value = 4, GradeType = GradeType.OralAnswer, GradedAt = period1.StartDate.AddDays(21), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Grade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student2.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, PeriodId = period1.Id, Value = 4, GradeType = GradeType.ControlWork, GradedAt = period1.StartDate.AddDays(14), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Grade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student2.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, PeriodId = period1.Id, Value = 5, GradeType = GradeType.Test, GradedAt = period1.StartDate.AddDays(28), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Grade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student3.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, PeriodId = period1.Id, Value = 3, GradeType = GradeType.ControlWork, GradedAt = period1.StartDate.AddDays(14), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            // Русский язык
            new Grade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student1.Id, TeacherId = admin.Id, SubjectId = subjectRussian.Id, PeriodId = period1.Id, Value = 4, GradeType = GradeType.OralAnswer, GradedAt = period1.StartDate.AddDays(7), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Grade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student2.Id, TeacherId = admin.Id, SubjectId = subjectRussian.Id, PeriodId = period1.Id, Value = 5, GradeType = GradeType.OralAnswer, GradedAt = period1.StartDate.AddDays(7), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Grade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student3.Id, TeacherId = admin.Id, SubjectId = subjectRussian.Id, PeriodId = period1.Id, Value = 4, GradeType = GradeType.OralAnswer, GradedAt = period1.StartDate.AddDays(7), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };

        context.Grades.AddRange(grades);
        await context.SaveChangesAsync();

        // Посещаемость (первые 5 занятий по математике)
        var attendanceRecords = new List<Attendance>();
        for (var day = 0; day < 5; day++)
        {
            var lessonDate = period1.StartDate.AddDays(day * 7).Date;
            attendanceRecords.Add(new Attendance { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student1.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, Date = lessonDate, Status = AttendanceStatus.Present, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            attendanceRecords.Add(new Attendance { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student2.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, Date = lessonDate, Status = day == 2 ? AttendanceStatus.Late : AttendanceStatus.Present, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            attendanceRecords.Add(new Attendance { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student3.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, Date = lessonDate, Status = day == 1 ? AttendanceStatus.Absent : AttendanceStatus.Present, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }

        context.Attendances.AddRange(attendanceRecords);
        await context.SaveChangesAsync();

        // Итоговые оценки за I четверть
        var finalGrades = new[]
        {
            new FinalGrade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student1.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, PeriodId = period1.Id, Value = 5, Comment = "Стабильно высокий результат", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new FinalGrade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student2.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, PeriodId = period1.Id, Value = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new FinalGrade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student3.Id, TeacherId = admin.Id, SubjectId = subjectMath.Id, PeriodId = period1.Id, Value = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new FinalGrade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student1.Id, TeacherId = admin.Id, SubjectId = subjectRussian.Id, PeriodId = period1.Id, Value = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new FinalGrade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student2.Id, TeacherId = admin.Id, SubjectId = subjectRussian.Id, PeriodId = period1.Id, Value = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new FinalGrade { Id = Guid.NewGuid(), OrganizationId = organization.Id, StudentId = student3.Id, TeacherId = admin.Id, SubjectId = subjectRussian.Id, PeriodId = period1.Id, Value = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };

        context.FinalGrades.AddRange(finalGrades);
        await context.SaveChangesAsync();
    }
}
