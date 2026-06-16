# Backend — Favilonia API

REST API платформы Favilonia: multi-tenant, JWT-авторизация, новости, расписание, страницы, обратная связь, регистрация организаций.

**Стек:** C# / ASP.NET Core 8 / PostgreSQL / Entity Framework Core 8.

---

## 🏗️ Структура (чистая многослойная архитектура)

```
Backend/
├── Favilonia.API/            ← веб-API: контроллеры, DTO, JWT, авторизация, middleware
├── Favilonia.Infrastructure/ ← EF Core: AppDbContext, миграции, seed-данные
├── Favilonia.Domain/         ← доменные сущности (Organization, User, News, Schedule, ...)
└── Favilonia.sln
```

Зависимости: `API → Infrastructure → Domain`. Бизнес-логика живёт в контроллерах и `AppDbContext`.

---

## 🚀 Запуск

> Требуется запущенный PostgreSQL на `localhost:5432` (см. строку подключения в [`Favilonia.API/appsettings.json`](Favilonia.API/appsettings.json)).

**Через скрипт (из корня репозитория):**
```
Documentation\Scripts\Backend\run-backend.bat
```

**Вручную:**
```powershell
cd Backend/Favilonia.API
dotnet run
```

API поднимется на **http://localhost:5011**, Swagger — на **http://localhost:5011/swagger**.

В режиме Development при старте **автоматически**:
- применяются миграции (создают БД `favilonia_db`, если её нет);
- заливаются демо-данные (организация, админ, новости, расписание).

Полная инструкция с нуля — [Documentation/01_Backend/Setup.md](../Documentation/01_Backend/Setup.md).

---

## 🔑 Авторизация и multi-tenancy

- Данные изолированы по `OrganizationId`; маршруты вида `api/organizations/{organizationId:guid}/...`.
- JWT содержит claim `organizationId`; политики: `SameOrganization`, `AdminOnly`, `SuperAdmin`.
- Аутентификация: `POST /api/auth/login` → access + refresh токены. Ротация — `POST /api/auth/refresh`, выход — `POST /api/auth/logout`.
- Онбординг: `POST /api/organizations/register` создаёт организацию + первого админа.

Подробно про изоляцию данных — [Documentation/01_Backend/Architecture/MultiTenant.md](../Documentation/01_Backend/Architecture/MultiTenant.md).

### Демо-учётные данные (из seed)
| Поле | Значение |
|------|----------|
| ID организации | `12345678-1234-1234-1234-123456789012` |
| Админ | `admin@demo-school.local` / `Admin@123456` |
| Пользователь | `user@demo-school.local` / `User@123456` |

---

## 🗄️ Миграции EF Core

DbContext лежит в `Favilonia.Infrastructure`, поэтому команды требуют указания проектов:

```powershell
# Создать миграцию
dotnet ef migrations add ИмяМиграции `
  --project Backend/Favilonia.Infrastructure `
  --startup-project Backend/Favilonia.API

# Применить к БД
dotnet ef database update `
  --project Backend/Favilonia.Infrastructure `
  --startup-project Backend/Favilonia.API
```

Или через скрипты: `Documentation\Scripts\Backend\create-migration.bat ИмяМиграции` и `migrate-db.bat`.

---

## ⚙️ Конфигурация

- [`appsettings.json`](Favilonia.API/appsettings.json) — строка подключения и базовые JWT-настройки.
- Секреты для прода (JWT-ключ) задавайте через переменную окружения `Jwt__Key`, а не в файле.
- `appsettings.Development.json` и `appsettings.Local.json` игнорируются git — для локальных переопределений.

---

## 📥 Клонировать только Backend (sparse-checkout)

Если нужен не весь репозиторий, используйте [`Documentation/Scripts/Git/clone-backend-only.bat`](../Documentation/Scripts/Git/clone-backend-only.bat) или вручную:
```powershell
git clone --no-checkout https://github.com/MR-Kenik/Favilonia.git
cd Favilonia
git sparse-checkout init --cone
git sparse-checkout set Backend Documentation
git checkout dev
```
