# Учебное руководство по проекту Favilonia

> Этот документ объясняет все технологии и приёмы, которые используются в проекте.
> Для каждого приёма указано: что это, зачем нужно, и в каком файле можно посмотреть живой пример.

---

## Что строится в этом проекте?

**Favilonia** — это мульти-тенантный REST API-бэкенд для учебных заведений (SaaS-платформа).
Проще говоря: один сервер обслуживает несколько школ/организаций одновременно, у каждой своя изолированная среда.

Стек технологий: **C# / ASP.NET Core 8 / Entity Framework Core / PostgreSQL / JWT**

---

## Архитектура проекта (слоистая / Clean Architecture)

Проект разбит на несколько отдельных библиотек (проектов):

| Проект | Назначение |
|---|---|
| `Favilonia.Domain` | Сущности (таблицы БД), бизнес-правила |
| `Favilonia.Infrastructure` | Работа с базой данных (EF Core, миграции) |
| `Favilonia.API` | HTTP-контроллеры, авторизация, DTO, сервисы |
| `Favilonia.Application` | Зарезервирован под бизнес-логику (пока пуст) |
| `Favilonia.Shared` | Общие утилиты |

Смысл: каждый слой зависит только от нижнего, а не от всего подряд. Это упрощает тестирование и масштабирование.

---

## 1. REST API

**Что это:** Стиль проектирования HTTP-интерфейсов. Каждый URL — это ресурс, а HTTP-методы (GET, POST, PUT, DELETE) определяют действие над ним.

**Технология:** ASP.NET Core Web API

**Как это выглядит в коде:**
```
GET    /api/organizations          → получить все организации
POST   /api/organizations          → создать организацию
GET    /api/organizations/{id}     → получить одну
PUT    /api/organizations/{id}     → обновить
DELETE /api/organizations/{id}     → удалить
```

**Файлы:** все файлы в `Backend/Favilonia.API/Controllers/`

Атрибут `[ApiController]` говорит ASP.NET Core, что это REST-контроллер. `[Route("api/[controller]")]` — автоматически подставляет имя класса в URL.

---

## 2. CRUD-операции

**Что это:** Create / Read / Update / Delete — базовые операции над данными. Это не технология, а паттерн.

**Файлы:** `OrganizationsController.cs`, `UsersController.cs`, `NewsController.cs`, `SchedulesController.cs`, `PagesController.cs`, `FeedbacksController.cs`

Каждый контроллер реализует полный набор:
- `[HttpGet]` → Read (список)
- `[HttpGet("{id}")]` → Read (один)
- `[HttpPost]` → Create
- `[HttpPut("{id}")]` → Update
- `[HttpDelete("{id}")]` → Delete

---

## 3. EF Core (Entity Framework Core)

**Что это:** ORM (Object-Relational Mapper) — библиотека, которая позволяет работать с базой данных через C#-объекты, а не писать SQL вручную.

**Технология:** Microsoft.EntityFrameworkCore + Npgsql (драйвер для PostgreSQL)

**Файлы:**
- `Backend/Favilonia.Infrastructure/Data/AppDbContext.cs` — центральный класс EF Core
- `Backend/Favilonia.Domain/Entities/` — все сущности (классы = таблицы)

```csharp
// Вместо SQL: SELECT * FROM Organizations WHERE Id = '...'
var org = await _db.Organizations.FindAsync(id);

// Вместо SQL: INSERT INTO Organizations...
_db.Organizations.Add(organization);
await _db.SaveChangesAsync();
```

`DbSet<T>` — это "таблица" в коде. `SaveChangesAsync()` — отправляет накопленные изменения в БД.

---

## 4. DbContext (контекст базы данных)

**Что это:** Главный класс EF Core, через который идут все запросы к БД. Знает про все таблицы и следит за изменениями объектов.

**Файл:** `Backend/Favilonia.Infrastructure/Data/AppDbContext.cs`

В `AppDbContext` есть:
- `DbSet<Organization> Organizations` — таблица организаций
- `DbSet<User> Users` — таблица пользователей
- `OnModelCreating` — настройки таблиц (индексы, фильтры)
- `SaveChangesAsync` — переопределён, чтобы автоматически обновлять `UpdatedAt`

---

## 5. Миграции EF Core

**Что это:** Механизм версионирования схемы базы данных. Вместо ручного изменения таблиц — создаёшь "миграцию" и запускаешь её.

**Файлы:** `Backend/Favilonia.Infrastructure/Favilonia.Infrastructure/Migrations/`

Каждый файл миграции — это снимок изменений в БД: какие таблицы создать, какие столбцы добавить.

Команды (из `Documentation/Scripts/Backend/`):
```
create-migration.bat  → создать новую миграцию
update-database.bat   → применить миграции к БД
```

---

## 6. Code First подход

**Что это:** Подход, при котором структура БД определяется C#-классами, а не наоборот. Ты пишешь класс `User` → EF Core создаёт таблицу `Users`.

**Файлы:** `Backend/Favilonia.Domain/Entities/` — все классы-сущности

```csharp
public class User : BaseEntity   // класс → таблица Users
{
    public string Email { get; set; }    // свойство → столбец
    public Guid OrganizationId { get; set; }  // внешний ключ
}
```

---

## 7. Наследование и базовый класс (BaseEntity)

**Что это:** Классический ООП-приём. Общие поля (`Id`, `CreatedAt`, `UpdatedAt`) вынесены в базовый класс, чтобы не повторять их в каждой сущности.

**Файл:** `Backend/Favilonia.Domain/Entities/BaseEntity.cs`

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

`abstract` — класс нельзя создать напрямую, только наследоваться от него. Все сущности (`User`, `Organization`, `News` и т.д.) наследуют эти поля автоматически.

---

## 8. DTO (Data Transfer Object)

**Что это:** Отдельные классы для передачи данных между слоями. Клиент получает не саму сущность БД, а "урезанную" копию с нужными полями.

**Зачем:** Чтобы не "светить" лишние поля (например, `PasswordHash`) и иметь контроль над тем, что принимается и отдаётся.

**Файлы:** `Backend/Favilonia.API/Dtos/` — для каждого ресурса свой:

- `CreateXxxRequest` — что принимает API при создании
- `UpdateXxxRequest` — что принимает при обновлении
- `XxxResponse` — что возвращает API

**Пример из `AuthDtos.cs`:**
```csharp
public class LoginRequest        // принимаем от клиента
{
    public Guid OrganizationId { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class AuthResponse        // отдаём клиенту
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Role { get; set; }
    // PasswordHash сюда не попадает!
}
```

---

## 9. JWT-аутентификация (JSON Web Token)

**Что это:** Стандарт для передачи "удостоверения личности" пользователя. Клиент логинится → получает токен → прикладывает его к каждому запросу в заголовке `Authorization: Bearer <token>`.

**Технология:** `Microsoft.AspNetCore.Authentication.JwtBearer`

**Файлы:**
- `Backend/Favilonia.API/Services/JwtTokenGenerator.cs` — генерация токена
- `Backend/Favilonia.API/Settings/JwtSettings.cs` — конфигурация (ключ, срок жизни)
- `Backend/Favilonia.API/Program.cs` — регистрация JWT в приложении

Токен содержит "клеймы" (claims) — информацию о пользователе:
```csharp
var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(ClaimTypes.Role, user.Role),
    new Claim("organizationId", user.OrganizationId.ToString())  // кастомный клейм
};
```

---

## 10. Claims (Клеймы / Утверждения)

**Что это:** Данные внутри JWT-токена о текущем пользователе. После проверки токена эти данные доступны через `User` (объект `ClaimsPrincipal`) в любом контроллере.

**Файл:** `Backend/Favilonia.API/Extensions/ClaimsPrincipalExtensions.cs`

```csharp
// Метод расширения — удобный способ читать клейм
public static Guid? GetOrganizationId(this ClaimsPrincipal user)
{
    var claim = user.FindFirst("organizationId")?.Value;
    return Guid.TryParse(claim, out var id) ? id : null;
}
```

В контроллере можно написать `User.GetOrganizationId()` и получить `Guid` организации текущего пользователя.

---

## 11. Авторизация и политики (Authorization Policies)

**Что это:** Механизм ограничения доступа к эндпоинтам. Можно разрешить/запретить по роли или по любому условию.

**Файлы:**
- `Backend/Favilonia.API/Authorization/AuthorizationPolicies.cs` — названия политик
- `Backend/Favilonia.API/Authorization/Roles.cs` — константы ролей
- `Backend/Favilonia.API/Authorization/OrganizationAuthorizationRequirement.cs` — требование политики
- `Backend/Favilonia.API/Authorization/OrganizationAuthorizationHandler.cs` — логика проверки
- `Backend/Favilonia.API/Extensions/ServiceCollectionExtensions.cs` — регистрация политик

Две политики в проекте:
- `AdminOnly` — только роль Admin
- `SameOrganization` — пользователь может обращаться только к данным своей организации

```csharp
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]   // только Admin
[Authorize(Policy = AuthorizationPolicies.SameOrganization)]  // только своя организация
[AllowAnonymous]   // вообще без авторизации
```

---

## 12. Custom Authorization Handler (кастомная авторизация)

**Что это:** Свой обработчик авторизации, который реализует нестандартную логику. Здесь — проверяет, что `organizationId` в URL совпадает с `organizationId` из JWT-токена пользователя.

**Файл:** `Backend/Favilonia.API/Authorization/OrganizationAuthorizationHandler.cs`

Это мульти-тенантная защита: даже аутентифицированный пользователь не сможет смотреть данные чужой организации.

---

## 13. Dependency Injection (Внедрение зависимостей / DI)

**Что это:** Паттерн, при котором объекты не создаются вручную внутри класса, а "вводятся" снаружи через конструктор. ASP.NET Core имеет встроенный DI-контейнер.

**Технология:** Microsoft.Extensions.DependencyInjection (встроено в ASP.NET Core)

**Файлы:**
- `Backend/Favilonia.API/Program.cs` — регистрация всех зависимостей
- `Backend/Favilonia.API/Extensions/ServiceCollectionExtensions.cs` — вынесенная регистрация

```csharp
// Регистрация (Program.cs)
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<JwtTokenGenerator>();

// Использование (контроллер — через конструктор)
public AuthController(AppDbContext db, JwtTokenGenerator tokenGenerator)
{
    _db = db;
    _tokenGenerator = tokenGenerator;
    // ASP.NET Core сам создаёт и передаёт эти объекты
}
```

Виды времени жизни:
- `AddScoped` — один объект на HTTP-запрос
- `AddSingleton` — один объект на всё время работы приложения
- `AddTransient` — новый объект каждый раз

---

## 14. Extension Methods (Методы расширения)

**Что это:** C#-фича, которая позволяет добавлять методы к существующим типам без изменения их исходного кода.

**Файлы:**
- `Backend/Favilonia.API/Extensions/ServiceCollectionExtensions.cs` — расширение `IServiceCollection`
- `Backend/Favilonia.API/Extensions/ClaimsPrincipalExtensions.cs` — расширение `ClaimsPrincipal`

```csharp
// Без расширения пришлось бы писать везде:
builder.Services.AddControllers(); builder.Services.AddAuthorization(); // и т.д.

// С расширением — один чистый вызов:
builder.Services.AddApiInfrastructure();
```

---

## 15. Middleware (Промежуточное ПО)

**Что это:** Компонент в цепочке обработки HTTP-запроса. Запрос проходит через middleware последовательно перед тем, как попасть в контроллер.

**Файл:** `Backend/Favilonia.API/Middleware/ApiExceptionHandlerMiddleware.cs`

Здесь middleware перехватывает все необработанные исключения и возвращает клиенту читаемый JSON-ответ вместо стека ошибок.

```
Запрос → HttpsRedirection → ExceptionHandler → Authentication → Authorization → Controller → Ответ
```

Порядок важен! Он задаётся в `Program.cs` через `app.UseXxx()`.

---

## 16. Soft Delete (Мягкое удаление)

**Что это:** Паттерн, при котором запись не удаляется физически из БД, а помечается флагом `IsDeleted = true`. Позволяет восстановить данные и сохранить историю.

**Файлы:**
- `Backend/Favilonia.Domain/Entities/Organization.cs` — поле `IsDeleted`
- `Backend/Favilonia.Domain/Entities/News.cs` — поле `IsDeleted`
- `Backend/Favilonia.Infrastructure/Data/AppDbContext.cs` — глобальный фильтр

```csharp
// В AppDbContext — фильтр автоматически исключает удалённые записи:
modelBuilder.Entity<Organization>()
    .HasQueryFilter(x => !x.IsDeleted);

// В контроллере — "удаляем":
organization.IsDeleted = true;
await _db.SaveChangesAsync();

// Чтобы увидеть удалённые записи — снимаем фильтр:
_db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
```

**Файлы Controllers:** `OrganizationsController.cs`, `NewsController.cs`, `SchedulesController.cs`, `PagesController.cs`

---

## 17. Global Query Filters (Глобальные фильтры запросов)

**Что это:** EF Core-фича. Условие фильтрации, которое автоматически добавляется ко всем запросам к определённой таблице. Не нужно писать `WHERE IsDeleted = false` везде вручную.

**Файл:** `Backend/Favilonia.Infrastructure/Data/AppDbContext.cs` — метод `OnModelCreating`

---

## 18. Пагинация (Pagination)

**Что это:** Разбивка большого списка данных на страницы. Вместо возврата 10 000 записей — возвращаешь 20 записей и номер страницы.

**Файлы:**
- `Backend/Favilonia.API/Dtos/Common/PaginationDtos.cs` — универсальный DTO
- `NewsController.cs`, `SchedulesController.cs`, `PagesController.cs`, `FeedbacksController.cs` — использование

```csharp
// Клиент передаёт: GET /api/.../news?page=2&pageSize=10
// Код:
.Skip((page - 1) * pageSize)  // пропустить первые N записей
.Take(pageSize)               // взять pageSize записей
```

`PaginationResponse<T>` — обобщённый (generic) класс. `T` — это тип элементов (например `NewsResponse`). Возвращает список элементов + метаданные (всего страниц, есть ли следующая и т.д.).

---

## 19. Generics (Обобщённые типы)

**Что это:** C#-фича. Позволяет писать классы и методы, которые работают с любым типом данных, подставляемым в момент использования.

**Файл:** `Backend/Favilonia.API/Dtos/Common/PaginationDtos.cs`

```csharp
public class PaginationResponse<T>   // T — любой тип
{
    public List<T> Items { get; set; }
}

// Использование:
PaginationResponse<NewsResponse>      // список новостей
PaginationResponse<ScheduleResponse>  // список расписаний
// Один класс — для всех ресурсов
```

---

## 20. Data Annotations (Валидация данных)

**Что это:** Атрибуты на свойствах DTO для валидации входящих данных. ASP.NET Core проверяет их автоматически перед вызовом метода контроллера.

**Технология:** `System.ComponentModel.DataAnnotations`

**Файл:** `Backend/Favilonia.API/Dtos/Auth/AuthDtos.cs`

```csharp
[Required(ErrorMessage = "Email обязателен.")]
[EmailAddress(ErrorMessage = "Неверный формат email.")]
[MaxLength(200)]
public string Email { get; set; }
```

Если данные невалидны, ASP.NET Core вернёт `400 Bad Request` автоматически. Кастомизация этого ответа — в `ServiceCollectionExtensions.cs`.

---

## 21. Custom Validation Attribute (Кастомный атрибут валидации)

**Что это:** Свой атрибут проверки данных, когда стандартных `[Required]`, `[MaxLength]` и т.д. недостаточно.

**Файл:** `Backend/Favilonia.API/Validation/AllowedRolesAttribute.cs`

```csharp
[AllowedRoles]  // проверяет, что роль = "Admin" или "User"
public string Role { get; set; }
```

---

## 22. Password Hashing (Хэширование паролей)

**Что это:** Преобразование пароля в нечитаемую строку (хэш) перед сохранением в БД. Нельзя восстановить пароль из хэша, можно только проверить совпадение.

**Технология:** BCrypt (через `BCrypt.Net`)

**Файлы:**
- `Backend/Favilonia.Infrastructure/Services/PasswordHasher.cs` — реализация
- `Backend/Favilonia.API/Services/PasswordHasher.cs` — обёртка для обратной совместимости

```csharp
PasswordHasher.Hash("mypassword")        // → "$2a$11$abc..."
PasswordHasher.Verify("mypassword", hash) // → true/false
```

---

## 23. Seed Data (Начальное заполнение БД)

**Что это:** Автоматическое создание тестовых данных при первом запуске приложения. Удобно для разработки.

**Файл:** `Backend/Favilonia.Infrastructure/Data/SeedData.cs`

При старте приложения создаётся демо-организация, администратор и тестовый пользователь. Логика в `Program.cs`:
```csharp
await SeedData.InitializeAsync(context);
```

---

## 24. Мульти-тенантность (Multi-Tenancy)

**Что это:** Архитектурный паттерн, при котором один экземпляр приложения обслуживает несколько независимых клиентов (тенантов). Каждый клиент — организация с изолированными данными.

**Как реализовано в проекте:**
- Все ресурсы имеют поле `OrganizationId`
- URL имеет вид `/api/organizations/{organizationId}/news` — organizationId всегда в маршруте
- Политика `SameOrganization` проверяет, что пользователь обращается только к данным своей организации

**Файлы:**
- Все сущности в `Backend/Favilonia.Domain/Entities/`
- `Backend/Favilonia.API/Authorization/OrganizationAuthorizationHandler.cs`

---

## 25. Nested Routes (Вложенные маршруты)

**Что это:** URL, в котором дочерний ресурс вложен в родительский. Это REST-практика для выражения зависимостей между ресурсами.

**Файлы:** `UsersController.cs`, `NewsController.cs`, `SchedulesController.cs`, `PagesController.cs`, `FeedbacksController.cs`

```
/api/organizations/{organizationId}/users       → пользователи организации
/api/organizations/{organizationId}/news        → новости организации
/api/organizations/{organizationId}/schedules   → расписания организации
```

`{organizationId:guid}` — route constraint, ограничивает тип параметра до GUID.

---

## 26. Async/Await (Асинхронное программирование)

**Что это:** Паттерн для неблокирующего выполнения операций ввода-вывода (запросы к БД, сети и т.д.). Пока ждёт БД — поток обрабатывает другие запросы.

**Файлы:** везде — все методы контроллеров и сервисов

```csharp
// Синхронно (плохо для веба):
var user = _db.Users.FirstOrDefault(x => x.Email == email);

// Асинхронно (правильно):
var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
```

Метод помечается `async`, возвращает `Task<T>`, вместо обычного `T`.

---

## 27. LINQ (Language Integrated Query)

**Что это:** C#-синтаксис для запросов к коллекциям и БД. EF Core преобразует LINQ в SQL.

**Файлы:** все контроллеры

```csharp
var news = await _db.News
    .Where(x => x.OrganizationId == organizationId)   // WHERE
    .OrderByDescending(x => x.CreatedAt)              // ORDER BY
    .Skip((page - 1) * pageSize)                      // OFFSET
    .Take(pageSize)                                   // LIMIT
    .Select(x => new NewsResponse { Title = x.Title }) // SELECT (проекция)
    .ToListAsync();                                   // выполнить запрос
```

---

## 28. Projection (Проекция / Select в LINQ)

**Что это:** Выборка только нужных полей из БД вместо загрузки всей сущности. Уменьшает объём данных, передаваемых из БД в приложение.

**Файлы:** все контроллеры (метод `.Select(x => new XxxResponse {...})`)

```csharp
// Плохо — грузит все поля, включая лишние:
var user = await _db.Users.FindAsync(id);

// Хорошо — грузит только то, что нужно:
var user = await _db.Users
    .Where(x => x.Id == id)
    .Select(x => new UserResponse { Email = x.Email, FullName = x.FullName })
    .FirstOrDefaultAsync();
```

---

## 29. Options Pattern (Паттерн настроек)

**Что это:** Способ читать конфигурацию из `appsettings.json` в строго типизированный C#-класс.

**Файлы:**
- `Backend/Favilonia.API/Settings/JwtSettings.cs` — класс с настройками
- `Backend/Favilonia.API/appsettings.json` — сами значения
- `Backend/Favilonia.API/Program.cs` — регистрация

```csharp
// appsettings.json:
"Jwt": { "Key": "...", "Issuer": "Favilonia", "ExpirationMinutes": 60 }

// Класс:
public class JwtSettings { public string Key; public string Issuer; public int ExpirationMinutes; }

// Регистрация:
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Использование (через конструктор):
public JwtTokenGenerator(IOptions<JwtSettings> settings) { _settings = settings.Value; }
```

---

## 30. Environment Variables (Переменные окружения)

**Что это:** Способ хранить секреты (пароли, ключи) вне кода и конфиг-файлов. Обязательно для продакшена.

**Файл:** `Backend/Favilonia.API/Program.cs`

```csharp
// Сначала ищет переменную окружения, потом падает на appsettings.json:
var secretKey = Environment.GetEnvironmentVariable("Jwt__Key")
    ?? builder.Configuration["Jwt:Key"];
```

`Jwt__Key` — двойное подчёркивание в переменных окружения = `:` в конфиге.

---

## 31. HTTP Status Codes (HTTP-коды ответов)

**Что это:** Стандартные коды, которые сервер возвращает клиенту, сообщая об успехе или ошибке.

**Файлы:** все контроллеры

| Метод | Код | Когда используется |
|---|---|---|
| `Ok(data)` | 200 | Успешный GET |
| `CreatedAtAction(...)` | 201 | Успешный POST (создание) |
| `NoContent()` | 204 | Успешный PUT/DELETE (без тела ответа) |
| `NotFound()` | 404 | Запись не найдена |
| `Conflict(...)` | 409 | Уникальный ключ уже существует |
| `Unauthorized(...)` | 401 | Не аутентифицирован |
| `Forbid()` | 403 | Нет прав |
| `BadRequest(...)` | 400 | Ошибка валидации |

---

## 32. Swagger / OpenAPI

**Что это:** Автоматически генерирует интерактивную документацию API прямо из кода. В браузере можно посмотреть все эндпоинты и отправить тестовые запросы.

**Файл:** `Backend/Favilonia.API/Program.cs`

```csharp
builder.Services.AddSwaggerGen();       // регистрация
app.UseSwagger();                       // включение
app.UseSwaggerUI();                     // веб-интерфейс
```

Доступен по адресу `https://localhost:5001/swagger` в режиме разработки.

---

## 33. Unique Indexes (Уникальные индексы в БД)

**Что это:** Ограничение на уровне БД, которое запрещает дублирующиеся значения в столбце. Работает быстрее, чем проверка в коде.

**Файл:** `Backend/Favilonia.Infrastructure/Data/AppDbContext.cs`

```csharp
// Домен организации уникален глобально:
modelBuilder.Entity<Organization>()
    .HasIndex(x => x.Domain)
    .IsUnique();

// Slug страницы уникален в рамках одной организации:
modelBuilder.Entity<Page>()
    .HasIndex(x => new { x.OrganizationId, x.Slug })
    .IsUnique();
```

---

## Быстрая шпаргалка: технологии проекта

| Технология/Приём | Где смотреть |
|---|---|
| REST API | `Controllers/*.cs` |
| CRUD | `Controllers/*.cs` |
| EF Core ORM | `Infrastructure/Data/AppDbContext.cs` |
| Миграции EF Core | `Infrastructure/.../Migrations/` |
| Code First | `Domain/Entities/` |
| BaseEntity | `Domain/Entities/BaseEntity.cs` |
| DTO | `API/Dtos/` |
| JWT-аутентификация | `API/Services/JwtTokenGenerator.cs`, `Program.cs` |
| Claims | `API/Extensions/ClaimsPrincipalExtensions.cs` |
| Authorization Policies | `API/Authorization/` |
| Custom Auth Handler | `API/Authorization/OrganizationAuthorizationHandler.cs` |
| Dependency Injection | `Program.cs`, `Extensions/ServiceCollectionExtensions.cs` |
| Extension Methods | `API/Extensions/` |
| Middleware | `API/Middleware/ApiExceptionHandlerMiddleware.cs` |
| Soft Delete | `Domain/Entities/` + `AppDbContext.cs` |
| Global Query Filters | `Infrastructure/Data/AppDbContext.cs` |
| Пагинация | `Dtos/Common/PaginationDtos.cs` + Controllers |
| Generics | `Dtos/Common/PaginationDtos.cs` |
| Data Annotations | `Dtos/Auth/AuthDtos.cs` |
| Custom Validation | `API/Validation/AllowedRolesAttribute.cs` |
| Password Hashing | `Infrastructure/Services/PasswordHasher.cs` |
| Seed Data | `Infrastructure/Data/SeedData.cs` |
| Multi-Tenancy | Все контроллеры + `Authorization/` |
| Nested Routes | `UsersController.cs`, `NewsController.cs` и др. |
| Async/Await | Все контроллеры |
| LINQ | Все контроллеры |
| Projection (Select) | Все контроллеры |
| Options Pattern | `API/Settings/JwtSettings.cs` + `Program.cs` |
| Environment Variables | `Program.cs` |
| HTTP Status Codes | Все контроллеры |
| Swagger / OpenAPI | `Program.cs` |
| Unique Indexes | `Infrastructure/Data/AppDbContext.cs` |
