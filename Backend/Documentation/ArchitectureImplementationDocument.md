# Архитектурные решения и реализация Favilonia Backend

Этот документ объединяет:
- Архитектурные решения (ADR) — **почему** выбраны определённые решения
- Имплементационные заметки — **что** и **как** было реализовано

## Структура документа

Каждый раздел содержит:
1. **Архитектурное решение (ADR)** — обоснование выбора
2. **Имплементация** — как это реализовано в коде
3. **Файлы** — какие файлы содержат реализацию
4. **Примеры кода** — показательные фрагменты

---

## 1. Архитектурный стиль: Монолит против микросервисов

### ADR-001: Монолитная архитектура
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Выбор архитектурного стиля для SaaS-платформы Favilonia.

**Решение:** Монолитная архитектура в единственном процессе ASP.NET Core.

**Причины:**
- **Скорость разработки** — один проект, одна сборка, одно развертывание
- **Простота отладки** — все в одном процессе, стек трейсы понятнее
- **Экономия ресурсов** — один сервер вместо десятков микросервисов
- **MVP приоритет** — фокус на функциональности, не на масштабировании

**Альтернативы:**
- Микросервисы (сложность, оверхед на раннем этапе)
- Serverless (vendor lock-in, холодные старты)

**Последствия:**
- Проще добавить фичи
- Сложнее независимо масштабировать части системы
- Может потребоваться рефакторинг при росте

### Имплементация
Проект разбит на несколько отдельных библиотек:
- `Favilonia.Domain` — сущности, бизнес-правила
- `Favilonia.Infrastructure` — работа с БД
- `Favilonia.API` — HTTP-контроллеры, авторизация
- `Favilonia.Application` — бизнес-логика (пока пуст)
- `Favilonia.Shared` — общие утилиты

**Файлы:** `Backend/Favilonia.sln` — решение содержит все проекты

---

## 2. BaseEntity для избежания дублирования

### ADR-002: BaseEntity для всех сущностей
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Каждая сущность должна иметь `Id`, `CreatedAt`, `UpdatedAt`. Это дублирование кода.

**Решение:** Создан абстрактный класс `BaseEntity` с этими полями.

**Причины:**
- **DRY принцип** — не повторяем поля в каждой сущности
- **Консистентность** — все сущности имеют одинаковую структуру
- **Аудит** — `CreatedAt` и `UpdatedAt` есть везде для отслеживания истории

### Имплементация
```csharp
// Favilonia.Domain/Entities/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

Все сущности наследуют BaseEntity:
```csharp
// Organization.cs
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    // ...
}

// User.cs  
public class User : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    // ...
}
```

**Файлы:**
- `Favilonia.Domain/Entities/BaseEntity.cs`
- Все файлы в `Favilonia.Domain/Entities/`

---

## 3. Soft Delete вместо Hard Delete

### ADR-003: Мягкое удаление с флагом IsDeleted
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Физическое удаление может привести к потере данных, которые могут понадобиться позже.

**Решение:** Добавить флаг `bool IsDeleted` и query filter в `DbContext`.

**Причины:**
- **Восстановление** — можно восстановить удаленные данные
- **Аудит и compliance** — регуляции требуют хранить историю
- **Ссылочная целостность** — Foreign Keys остаются валидными

### Имплементация

#### 1. Добавление поля IsDeleted в сущности
```csharp
public class Organization : BaseEntity
{
    public bool IsDeleted { get; set; } = false;
    // ...
}
```

#### 2. Query Filter в DbContext
```csharp
// Favilonia.Infrastructure/Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Organization>()
        .HasQueryFilter(x => !x.IsDeleted);
    // ... и для других сущностей
}
```

#### 3. Мягкое удаление в контроллерах
```csharp
// Вместо _db.Remove(organization)
organization.IsDeleted = true;
await _db.SaveChangesAsync();
```

#### 4. Получение удалённых записей
```csharp
var deletedOrgs = await _db.Organizations
    .IgnoreQueryFilters()
    .Where(x => x.IsDeleted == true)
    .ToListAsync();
```

**Файлы:**
- Все сущности в `Favilonia.Domain/Entities/` с полем `IsDeleted`
- `Favilonia.Infrastructure/Data/AppDbContext.cs` — query filters
- Контроллеры DELETE endpoints

---

## 4. JWT аутентификация

### ADR-004: JWT вместо Sessions
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Как аутентифицировать API запросы?

**Решение:** Использовать JWT (JSON Web Token).

**Причины:**
- **Масштабируемость** — нет нужды хранить sessions (stateless)
- **Мобильные приложения** — JWT удобнее для мобильных клиентов
- **CORS friendly** — работает с любым доменом (нет cookie restrictions)

### Имплементация

#### 1. Настройка JWT в Program.cs
```csharp
// Favilonia.API/Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });
```

#### 2. JwtTokenGenerator сервис
```csharp
// Favilonia.API/Services/JwtTokenGenerator.cs
public class JwtTokenGenerator
{
    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("organizationId", user.OrganizationId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### 3. AuthController для логина
```csharp
// Favilonia.API/Controllers/AuthController.cs
[HttpPost("login")]
public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
{
    var user = await _db.Users.FirstOrDefaultAsync(x => 
        x.OrganizationId == request.OrganizationId && x.Email == request.Email);
    
    if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
    {
        return Unauthorized(new { Message = "Неверные учетные данные." });
    }

    var token = _tokenGenerator.GenerateToken(user);
    return Ok(new AuthResponse { Token = token, ExpiresAt = ... });
}
```

**Файлы:**
- `Favilonia.API/Program.cs` — настройка JWT
- `Favilonia.API/Services/JwtTokenGenerator.cs` — генерация токена
- `Favilonia.API/Controllers/AuthController.cs` — эндпоинт логина
- `Favilonia.API/Dtos/Auth/AuthDtos.cs` — DTO для авторизации
- `Favilonia.API/Settings/JwtSettings.cs` — настройки

---

## 5. Мульти-тенантная архитектура

### ADR-005: Multi-tenant с OrganizationId везде
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Как разделить данные между организациями?

**Решение:** Каждая бизнес-сущность имеет `Guid OrganizationId`.

**Причины:**
- **Простота** — один database для всех, делим логически
- **Дешевизна** — не нужны отдельные БД для каждого клиента
- **Масштабируемость** — легче добавлять новых клиентов

### Имплементация

#### 1. Вложенные маршруты
```csharp
// Маршруты:
/api/organizations/{orgId}/news
/api/organizations/{orgId}/pages
/api/organizations/{orgId}/users
/api/organizations/{orgId}/schedules
```

#### 2. OrganizationId во всех сущностях
```csharp
// Все сущности имеют:
public Guid OrganizationId { get; set; }
```

#### 3. Автоматическая фильтрация
```csharp
// В контроллерах:
var news = await _db.News
    .Where(x => x.OrganizationId == organizationId)  // всегда фильтруем
    .ToListAsync();
```

#### 4. Политика SameOrganization
```csharp
// Favilonia.API/Authorization/OrganizationAuthorizationHandler.cs
public class OrganizationAuthorizationHandler 
    : AuthorizationHandler<OrganizationAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        OrganizationAuthorizationRequirement requirement)
    {
        // Проверяет, что organizationId из токена = organizationId из маршрута
        var orgFromToken = context.User.FindFirst("organizationId")?.Value;
        var orgFromRoute = authorizationContext.RouteData.Values["organizationId"]?.ToString();
        
        if (orgFromToken == orgFromRoute)
        {
            context.Succeed(requirement);
        }
    }
}
```

**Файлы:**
- Все контроллеры в `Favilonia.API/Controllers/`
- Все сущности в `Favilonia.Domain/Entities/`
- `Favilonia.API/Authorization/OrganizationAuthorizationHandler.cs`

---

## 6. DTO для разделения API контракта и Domain моделей

### ADR-006: DTO вместо возврата Domain моделей
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Возвращать Domain модели напрямую опасно: клиент видит все поля.

**Решение:** Создать DTO классы для каждого endpoint'а.

**Причины:**
- **API контракт** — отделяем внутреннее представление от публичного API
- **Валидация** — DataAnnotations валидируют входные данные
- **Гибкость** — можем менять Domain модель без влияния на API
- **Безопасность** — не экспортируем внутренние поля

### Имплементация

#### 1. Структура DTO
```
Favilonia.API/Dtos/
├── Auth/
│   └── AuthDtos.cs        // LoginRequest, AuthResponse
├── Common/
│   └── PaginationDtos.cs  // PaginationResponse<T>
├── News/
│   └── NewsDtos.cs        // CreateNewsRequest, UpdateNewsRequest, NewsResponse
└── Users/
    └── UserDtos.cs        // CreateUserRequest, UpdateUserRequest, UserResponse
```

#### 2. Пример DTO
```csharp
// AuthDtos.cs
public class LoginRequest
{
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Неверный формат email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов.")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

#### 3. Маппинг в контроллерах
```csharp
var news = new News
{
    Title = request.Title,
    Content = request.Content,
    PublishedAt = request.PublishedAt
};

var response = new NewsResponse
{
    Id = news.Id,
    Title = news.Title,
    Content = news.Content,
    PublishedAt = news.PublishedAt
};
```

**Файлы:** Все файлы в `Favilonia.API/Dtos/`

---

## 7. Пагинация

### Имплементация (из ImplementationNotes.md)

**Задача:** Реализовать пагинацию для новостей и расписаний.

#### 1. Pagination DTO
```csharp
// Favilonia.API/Dtos/Common/PaginationDtos.cs
public class PaginationResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
}
```

#### 2. Использование в контроллерах
```csharp
// NewsController.cs
[HttpGet]
public async Task<ActionResult<PaginationResponse<NewsResponse>>> GetAll(
    Guid organizationId, 
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 20)
{
    // Валидация
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 20;
    if (pageSize > 100) pageSize = 100;

    var totalCount = await _db.News
        .Where(x => x.OrganizationId == organizationId)
        .CountAsync();

    var news = await _db.News
        .Where(x => x.OrganizationId == organizationId)
        .OrderByDescending(x => x.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new NewsResponse
        {
            Id = x.Id,
            Title = x.Title,
            Content = x.Content,
            PublishedAt = x.PublishedAt
        })
        .ToListAsync();

    return Ok(new PaginationResponse<NewsResponse>(news, totalCount, page, pageSize));
}
```

#### 3. Запрос клиента
```bash
GET /api/organizations/{orgId}/news?page=2&pageSize=10
```

**Файлы:**
- `Favilonia.API/Dtos/Common/PaginationDtos.cs`
- `Favilonia.API/Controllers/NewsController.cs`
- `Favilonia.API/Controllers/SchedulesController.cs`
- `Favilonia.API/Controllers/PagesController.cs`

---

## 8. Безопасность: JWT Key в переменных окружения

### Имплементация

**Задача:** Вынести JWT ключ из конфигурационных файлов.

#### 1. Program.cs
```csharp
// Сначала переменная окружения, потом конфиг
var secretKey = Environment.GetEnvironmentVariable("Jwt__Key") 
    ?? builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 16)
{
    throw new InvalidOperationException(
        "JWT key must be configured via environment variable 'Jwt__Key'...");
}
```

#### 2. appsettings.json
```json
{
  "Jwt": {
    "Key": "DO_NOT_USE_IN_PRODUCTION_SET_JWT__KEY_ENV_VAR",
    "Issuer": "Favilonia",
    "Audience": "Favilonia",
    "ExpirationMinutes": 60
  }
}
```

#### 3. appsettings.Development.json (.gitignored)
```json
{
  "Jwt": {
    "Key": "development-secret-key-32-chars-long",
    "Issuer": "Favilonia",
    "Audience": "Favilonia",
    "ExpirationMinutes": 60
  }
}
```

**Файлы:**
- `Favilonia.API/Program.cs`
- `Favilonia.API/appsettings.json`
- `Favilonia.API/appsettings.Development.json`
- `.gitignore`

---

## 9. RBAC и централизованная проверка прав

### ADR-009: Пассивная авторизация через Claims
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Как проверять права доступа?

**Решение:** Использовать `[Authorize]` атрибуты с политиками.

### Имплементация

#### 1. Роли
```csharp
// Favilonia.API/Authorization/Roles.cs
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
```

#### 2. Политики
```csharp
// Favilonia.API/Authorization/AuthorizationPolicies.cs
public static class AuthorizationPolicies
{
    public const string SameOrganization = "SameOrganization";
    public const string AdminOnly = "AdminOnly";
}
```

#### 3. Регистрация политик
```csharp
// Favilonia.API/Extensions/ServiceCollectionExtensions.cs
services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.SameOrganization, policy =>
        policy.Requirements.Add(new OrganizationAuthorizationRequirement()));

    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(Roles.Admin));
});
```

#### 4. Использование в контроллерах
```csharp
[HttpGet]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]  // своя организация
public async Task<ActionResult> GetAll(Guid organizationId)

[HttpPost]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]  // только админ
public async Task<ActionResult> Create(...)
```

**Файлы:**
- `Favilonia.API/Authorization/`
- `Favilonia.API/Extensions/ServiceCollectionExtensions.cs`
- Все контроллеры с атрибутами `[Authorize]`

---

## 10. Глобальный обработчик ошибок

### ADR-011: Единый ErrorResponse формат
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Разные ошибки возвращаются в разных форматах.

**Решение:** Все ошибки возвращаются в одном формате: `ErrorResponse`.

### Имплементация

#### 1. Middleware обработчик
```csharp
// Favilonia.API/Middleware/ApiExceptionHandlerMiddleware.cs
public class ApiExceptionHandlerMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ошибка при обработке запроса");
            
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ErrorResponse
            {
                Message = "Внутренняя ошибка сервера. Попробуйте позже."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
```

#### 2. ErrorResponse классы
```csharp
// Favilonia.API/Responses/ErrorResponse.cs
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int Status { get; set; }
    public object? Details { get; set; }
}

public class ValidationErrorResponse : ErrorResponse
{
    public ValidationError[] Errors { get; set; } = Array.Empty<ValidationError>();
}
```

#### 3. Валидационные ошибки
```csharp
// ServiceCollectionExtensions.cs
services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .SelectMany(x => x.Value!.Errors.Select(error => new ValidationError
                {
                    Field = x.Key,
                    Message = error.ErrorMessage
                }))
                .ToArray();

            var response = new ValidationErrorResponse
            {
                Message = "Проверка данных не пройдена.",
                Status = StatusCodes.Status400BadRequest,
                Errors = errors
            };

            return new BadRequestObjectResult(response);
        };
    });
```

**Файлы:**
- `Favilonia.API/Middleware/ApiExceptionHandlerMiddleware.cs`
- `Favilonia.API/Responses/ErrorResponse.cs`
- `Favilonia.API/Extensions/ServiceCollectionExtensions.cs`

---

## 11. Seed Data механизм

### ADR-008: Seed Data в Application startup
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Нужны тестовые данные для разработки.

**Решение:** Вызвать `SeedData.InitializeAsync()` в `Program.cs`.

### Имплементация

#### 1. SeedData класс
```csharp
// Favilonia.Infrastructure/Data/SeedData.cs
public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (await context.Organizations.AnyAsync())
            return; // БД уже заполнена

        // Создаём тестовую организацию
        var organization = new Organization
        {
            Id = new Guid("12345678-1234-1234-1234-123456789012"),
            Name = "Демо Школа",
            Domain = "demo-school"
        };

        // Создаём администратора
        var admin = new User
        {
            Id = new Guid("87654321-4321-4321-4321-210987654321"),
            OrganizationId = organization.Id,
            Email = "admin@demo-school.local",
            PasswordHash = PasswordHasher.Hash("Admin@123456"),
            FullName = "Администратор",
            Role = "Admin"
        };

        // Добавляем новости, расписания...
        context.Organizations.Add(organization);
        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
```

#### 2. Интеграция в Program.cs
```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(context);
}
```

**Файлы:**
- `Favilonia.Infrastructure/Data/SeedData.cs`
- `Favilonia.API/Program.cs`

---

## 12. Модуль статических страниц (Page)

### ADR-012: Page Module с Slug
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Как обращаться к страницам?

**Решение:** Страницы имеют уникальный `slug`.

### Имплементация

#### 1. Page сущность
```csharp
// Favilonia.Domain/Entities/Page.cs
public class Page : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;  // например "about-school"
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
}
```

#### 2. DTO с валидацией
```csharp
// Favilonia.API/Dtos/Pages/PageDtos.cs
public class CreatePageRequest
{
    [Required(ErrorMessage = "Заголовок обязателен.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug обязателен.")]
    [RegularExpression(@"^[a-z0-9\-]+$", 
        ErrorMessage = "Slug содержит только строчные буквы, цифры и дефис.")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; }
}
```

#### 3. Контроллер
```csharp
// Favilonia.API/Controllers/PagesController.cs
[ApiController]
[Route("api/organizations/{organizationId:guid}/[controller]")]
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]
public class PagesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginationResponse<PageResponse>>> GetAll(...)
    
    [HttpPost]
    public async Task<ActionResult<PageResponse>> Create(...)
}
```

**Файлы:**
- `Favilonia.Domain/Entities/Page.cs`
- `Favilonia.API/Dtos/Pages/PageDtos.cs`
- `Favilonia.API/Controllers/PagesController.cs`

---

## 13. Модуль обратной связи (Feedback)

### ADR-013: Feedback Module без авторизации для отправки
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Проблема:** Как получать обратную связь от посетителей без регистрации?

**Решение:** `POST /feedbacks` доступен без авторизации.

### Имплементация

#### 1. Feedback сущность
```csharp
// Favilonia.Domain/Entities/Feedback.cs
public class Feedback : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
```

#### 2. Контроллер с AllowAnonymous
```csharp
// Favilonia.API/Controllers/FeedbacksController.cs
[ApiController]
[Route("api/organizations/{organizationId:guid}/[controller]")]
public class FeedbacksController : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]  // Отправка без авторизации
    public async Task<ActionResult<FeedbackResponse>> Create(...)
    
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SameOrganization)]  // Просмотр только админами
    public async Task<ActionResult<PaginationResponse<FeedbackResponse>>> GetAll(...)
}
```

**Файлы:**
- `Favilonia.Domain/Entities/Feedback.cs`
- `Favilonia.API/Dtos/Feedbacks/FeedbackDtos.cs`
- `Favilonia.API/Controllers/FeedbacksController.cs`

---

## 14. Русскоязычные сообщения

### ADR-010: Русскоязычные сообщения об ошибках
**Статус:** Accepted  
**Дата:** 2 июня 2026 г.

**Решение:** Все сообщения об ошибках, валидации и исключений на русском языке.

### Имплементация
```csharp
// Везде в DataAnnotations:
[Required(ErrorMessage = "Email обязателен.")]
[EmailAddress(ErrorMessage = "Неверный формат email.")]
[MaxLength(200, ErrorMessage = "Email не может быть длиннее 200 символов.")]
```

---

## Краткая шпаргалка по архитектуре

### Структура проекта
```
Backend/
├── Favilonia.Domain/          # Сущности (Entities)
├── Favilonia.Infrastructure/  # БД, миграции, SeedData
├── Favilonia.API/            # Контроллеры, DTO, JWT
├── Favilonia.Application/     # Бизнес-логика (пока пуст)
└── Favilonia.Shared/         # Общие утилиты
```

### Основные паттерны и технологии
1. **Монолит** — одна сборка, одно развертывание
2. **Multi-tenancy** — OrganizationId везде, вложенные маршруты
3. **BaseEntity** — DRY для общих полей
4. **Soft Delete** — IsDeleted + Query Filters
5. **JWT** — stateless аутентификация
6. **DTO** — отдельный API контракт
7. **Пагинация** — PaginationResponse<T>
8. **Авторизация** — Claims + Policies
9. **Глобальная обработка ошибок** — ApiExceptionHandlerMiddleware
10. **Seed Data** — авто-заполнение БД

### Главные файлы для изучения
- `Favilonia.API/Program.cs` — настройка приложения
- `Favilonia.Infrastructure/Data/AppDbContext.cs` — EF Core контекст
- `Favilonia.Domain/Entities/BaseEntity.cs` — базовая сущность
- `Favilonia.API/Authorization/OrganizationAuthorizationHandler.cs` — multi-tenant авторизация
- `Favilonia.API/Middleware/ApiExceptionHandlerMiddleware.cs` — обработка ошибок
- Любой контроллер в `Favilonia.API/Controllers/`

### Технологии
- ASP.NET Core 8
- Entity Framework Core + PostgreSQL
- JWT Bearer Authentication
- Swagger/OpenAPI
- DataAnnotations для валидации
- Dependency Injection

---

## Эволюция решений

| Версия | Основные изменения |
|---|---|
| MVP 1.0 | Организации, Новости, Расписания, Пользователи |
| MVP 2.0 | + Soft Delete, JWT env vars, BaseEntity |
| MVP 3.0 | + RBAC, глобальная обработка ошибок, русские сообщения |
| MVP 4.0 | + Страницы (Page), Обратная связь (Feedback) |

---

## Будущие улучшения

1. **Рефакторинг на микросервисы** (если система вырастет)
2. **Кеширование** (Redis)
3. **Background jobs** (Hangfire)
4. **GraphQL** (вместо REST)
5. **Версионирование API** (v1, v2)
6. **Логирование и мониторинг** (Serilog, ELK)

---

## Заключение

Favilonia использует классическую N-
слойную монолитную архитектуру с мульти-тенантностью, полным набором CRUD операций через REST API, и множеством паттернов для безопасности и удобства разработки.

Архитектурные решения (ADR) фиксируют **почему**, а ImplementationNotes документируют **что** и **как** было сделано.