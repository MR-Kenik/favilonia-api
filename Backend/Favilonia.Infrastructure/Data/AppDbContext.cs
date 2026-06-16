using Microsoft.EntityFrameworkCore;
using Favilonia.Domain.Entities;

namespace Favilonia.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<News> News => Set<News>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Электронный журнал
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<FinalGrade> FinalGrades => Set<FinalGrade>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        // CreatedAt и UpdatedAt проставляются автоматически — вручную их не задавай.
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Мягкое удаление: записи с IsDeleted = true не видны ни в одном обычном запросе.
        // Чтобы обратиться к уже удалённой записи (например в методе Delete),
        // нужно явно указать .IgnoreQueryFilters().
        modelBuilder.Entity<Organization>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<News>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Schedule>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Page>()
            .HasQueryFilter(x => !x.IsDeleted);

        // Уникальный домен — публичный адрес учреждения (например "school-123").
        modelBuilder.Entity<Organization>()
            .HasIndex(x => x.Domain)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => new { x.OrganizationId, x.Email })
            .IsUnique();

        modelBuilder.Entity<News>()
            .HasIndex(x => x.OrganizationId);

        modelBuilder.Entity<Schedule>()
            .HasIndex(x => x.OrganizationId);

        modelBuilder.Entity<Page>()
            .HasIndex(x => new { x.OrganizationId, x.Slug })
            .IsUnique();

        modelBuilder.Entity<Feedback>()
            .HasIndex(x => x.OrganizationId);

        // --- Электронный журнал ---

        modelBuilder.Entity<Group>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Group>()
            .HasIndex(x => x.OrganizationId);

        // GroupId на User — nullable, т.к. пользователи-администраторы группе не принадлежат
        modelBuilder.Entity<User>()
            .HasOne(x => x.Group)
            .WithMany(x => x.Students)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Subject>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Subject>()
            .HasIndex(x => x.OrganizationId);

        modelBuilder.Entity<Period>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Period>()
            .HasIndex(x => x.OrganizationId);

        // FinalGrade — уникальная итоговая оценка на студента/предмет/период
        modelBuilder.Entity<FinalGrade>()
            .HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FinalGrade>()
            .HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FinalGrade>()
            .HasIndex(x => new { x.StudentId, x.SubjectId, x.PeriodId }).IsUnique();
        modelBuilder.Entity<FinalGrade>()
            .HasIndex(x => x.OrganizationId);

        // Два FK на User из одной таблицы — Restrict предотвращает конфликт каскадного удаления.
        modelBuilder.Entity<Grade>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasIndex(x => new { x.OrganizationId, x.StudentId });

        modelBuilder.Entity<Grade>()
            .HasIndex(x => x.SubjectId);

        modelBuilder.Entity<Attendance>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Один статус посещаемости на студента/предмет/день — дубли запрещены.
        modelBuilder.Entity<Attendance>()
            .HasIndex(x => new { x.StudentId, x.SubjectId, x.Date })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasIndex(x => x.OrganizationId);

        // --- PasswordResetToken ---

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token).IsRequired();

            // Уникальный индекс — токен используется как идентификатор в ссылке сброса пароля.
            entity.HasIndex(x => x.Token).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(x => x.IsValid);
        });

        // --- RefreshToken ---

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token)
                .IsRequired();

            entity.HasIndex(x => x.Token)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // IsActive — вычисляемое свойство, колонки в БД нет.
            entity.Ignore(x => x.IsActive);
        });
    }
}