# Favilonia — мультитенантный SaaS для образовательных учреждений

REST API бэкенд SaaS-платформы для школ и образовательных организаций. Каждый клиент (организация) полностью изолирован на уровне данных — один экземпляр API обслуживает неограниченное количество организаций одновременно.

Стек: **ASP.NET Core 8** · **PostgreSQL** · **Entity Framework Core** · **Docker**

---

## Технологии

| Слой | Технология |
|---|---|
| Фреймворк | ASP.NET Core 8 Web API |
| База данных | PostgreSQL 16 + Entity Framework Core 8 |
| Авторизация | JWT Bearer + ротируемые Refresh-токены |
| Контейнеризация | Docker + Docker Compose |
| ORM | EF Core, Code-First миграции |
| Валидация | FluentValidation |

---

## Архитектурные особенности

### Мультитенантность
Каждый тенант-специфичный эндпоинт маршрутизируется через `/api/organizations/{organizationId}/...` и защищён кастомной политикой авторизации `SameOrganization`, которая сравнивает параметр маршрута с клеймом `organizationId` в JWT. Контроллеры дополнительно фильтруют каждый запрос по `OrganizationId` — эшелонированная защита.

```
Маршрут: /api/organizations/{organizationId:guid}/[controller]
Политика: SameOrganization  →  OrganizationAuthorizationHandler
Запрос:   .Where(x => x.OrganizationId == organizationId)
```

### Авторизация и токены
- Логин требует `organizationId + email + password` — составной ключ изолирует тенанты
- Access-токен: время жизни ~60 мин, содержит клеймы `organizationId` и `role`
- Refresh-токен: 7 дней, хранится в БД, **ротируется при каждом использовании**
- Сброс пароля: одноразовый токен с TTL 24ч, доставка через `IEmailService` (реализован консольный стаб — замени на SMTP)

### Роли
| Роль | Доступ |
|---|---|
| `SuperAdmin` | Все организации |
| `Admin` | Своя организация — полный CRUD |
| `User` (студент) | Своя организация — только свои данные (оценки, посещаемость) |

Изоляция студента — принудительная подмена `studentId` из JWT на общих эндпоинтах, чтобы студент не мог читать данные других:
```csharp
if (User.IsInRole(Roles.User))
    studentId = User.GetUserId();
```

### Слой данных
- `BaseEntity` предоставляет `Id / CreatedAt / UpdatedAt`; временны́е метки проставляются автоматически в `SaveChanges`
- Мягкое удаление через глобальные фильтры запросов (`IsDeleted`) на Organization, News, Schedule, Page, Subject, Group, Period
- Два FK на одну таблицу (например `Grade.StudentId` + `Grade.TeacherId`) → `DeleteBehavior.Restrict` на обоих, чтобы избежать конфликта EF-каскадов
- Централизованная обработка ошибок: `ApiExceptionHandlerMiddleware` + кастомная фабрика ответов на ошибки валидации

---

## Структура проекта

```
Backend/
├── Favilonia.API/
│   ├── Authorization/        # Политики SameOrganization, AdminOnly, SuperAdmin
│   ├── Controllers/          # REST-эндпоинты
│   ├── Dtos/                 # Модели запросов и ответов
│   ├── Extensions/           # Расширения IServiceCollection и ClaimsPrincipal
│   ├── Middleware/           # Глобальный обработчик исключений
│   ├── Services/             # JwtTokenGenerator, RefreshTokenService, EmailService
│   └── Validation/           # Валидаторы FluentValidation
├── Favilonia.Domain/
│   └── Entities/             # Доменные модели (Organization, User, Grade, Attendance …)
└── Favilonia.Infrastructure/
    ├── Data/
    │   ├── AppDbContext.cs
    │   ├── Migrations/
    │   └── Seed/             # Демо-организация и пользователи (только Development)
    └── Favilonia.Infrastructure.csproj
```

---

## Обзор API

### Публичные (без авторизации)
```
GET  /api/public/{domain}              — информация об организации по домену
GET  /api/public/{domain}/news         — опубликованные новости
GET  /api/public/{domain}/schedule     — предстоящие события
GET  /api/public/{domain}/pages/{slug} — страница по slug
```

### Аутентификация
```
POST /api/auth/login            — возвращает access + refresh токен
POST /api/auth/refresh          — ротация refresh-токена
POST /api/auth/logout           — отзыв refresh-токена
POST /api/auth/forgot-password  — генерация токена сброса (логируется в консоль)
POST /api/auth/reset-password   — проверка токена, смена пароля
```

### Онбординг
```
POST /api/organizations/register — создание организации + первого администратора, возвращает токены
```

### Тенант-специфичные (требуют политику SameOrganization)
```
/api/organizations/{orgId}/users
/api/organizations/{orgId}/news
/api/organizations/{orgId}/schedule
/api/organizations/{orgId}/groups
/api/organizations/{orgId}/subjects
/api/organizations/{orgId}/periods
/api/organizations/{orgId}/grades          + GET /summary
/api/organizations/{orgId}/attendance      + POST /bulk
/api/organizations/{orgId}/final-grades
/api/organizations/{orgId}/pages
/api/organizations/{orgId}/feedback
```

Полный Swagger UI доступен по адресу `/swagger` в режиме Development.

---

## Запуск

### Docker (рекомендуется — включает PostgreSQL)

```bash
cp .env.example .env
# Отредактируй .env: задай POSTGRES_PASSWORD и JWT_KEY
docker compose up --build
```

API: http://localhost:5011  
Swagger: http://localhost:5011/swagger

### Без Docker

Требуется PostgreSQL на `localhost:5432`. Задай строку подключения и JWT-ключ:

```bash
# В appsettings.json или переменных окружения:
# ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=favilonia_db;Username=postgres;Password=yourpassword"
# Jwt__Key = "секретный-ключ-минимум-16-символов"

dotnet run --project Backend/Favilonia.API
```

База данных создаётся и засевается автоматически при первом запуске (режим Development).

### Добавление EF-миграции

```bash
dotnet ef migrations add <Название> \
  --project Backend/Favilonia.Infrastructure \
  --startup-project Backend/Favilonia.API
```

---

## Демо-данные

Засеваются автоматически в режиме Development:

| Роль | Email | Пароль |
|---|---|---|
| Администратор | admin@demo-school.local | Admin@123456 |
| Студент | ivanov@demo-school.local | User@123456 |
| Студент | petrova@demo-school.local | User@123456 |

Домен организации: `demo-school`  
ID организации: `12345678-1234-1234-1234-123456789012`

---

## Конфигурация

| Переменная | Описание |
|---|---|
| `ConnectionStrings__DefaultConnection` | Строка подключения к PostgreSQL |
| `Jwt__Key` | Секретный ключ JWT (минимум 16 символов) |
| `Jwt__ExpirationMinutes` | Время жизни access-токена (по умолчанию: 60) |
| `Jwt__RefreshTokenExpirationDays` | Время жизни refresh-токена (по умолчанию: 7) |

Задаются через переменные окружения, `appsettings.json` или файл `.env` (Docker).

---

## Лицензия

MIT
