# Бэкенд: что доделать для публичного сайта учреждения

> Статус фронтенда: админ-кабинет учреждения готов (новости, расписание, страницы,
> обратная связь, пользователи, настройки). Публичный сайт **заблокирован** —
> у API нет анонимного чтения и резолва организации по домену.

---

## 1. Главное — публичный анонимный слой чтения

Сейчас `NewsController`, `SchedulesController`, `PagesController` имеют на уровне
класса `[Authorize(Policy = SameOrganization)]`. Анонимный посетитель сайта школы
не может прочитать ни новости, ни расписание, ни страницы. Плюс весь API работает
по `organizationId` (GUID), а публичный сайт знает только **домен**.

**Решение:** новый `PublicController`, всё `[AllowAnonymous]`, отдаёт **только
опубликованный** контент и резолвит организацию по `domain`.

```
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
```

### Эндпоинты

| Метод | Путь | Возвращает | Фильтр |
|---|---|---|---|
| GET | `/api/public/{domain}` | `{ id, name }` | 404 если нет/IsDeleted |
| GET | `/api/public/{domain}/news?page=&pageSize=` | `PaginationResponse<NewsResponse>` | только `PublishedAt != null`, сортировка по `PublishedAt` desc |
| GET | `/api/public/{domain}/schedule?page=&pageSize=` | `PaginationResponse<ScheduleResponse>` | см. примечание о расписании |
| GET | `/api/public/{domain}/pages` | список `{ title, slug }` | только `IsPublished` |
| GET | `/api/public/{domain}/pages/{slug}` | `PageResponse` | только `IsPublished`, иначе 404 |

### Примечания по реализации
- **Резолв домена** один раз в начале:
  `var org = await _db.Organizations.FirstOrDefaultAsync(x => x.Domain == domain);`
  `if (org is null) return NotFound();` — дальше всё фильтровать по `org.Id`.
- **Soft-delete** уже учитывается глобальным query-фильтром (контроллеры используют
  `IgnoreQueryFilters()` только в Delete) — обычные выборки сами исключают удалённое.
- **Расписание**: у `Schedule` нет флага публикации. Решить — отдавать всё или только
  актуальное (`EndDate >= DateTime.UtcNow`). Рекомендация: только актуальное.
- **Пагинацию** валидировать как в остальных контроллерах (`page<1→1`, `pageSize` 1..100).
- **Не отдавать черновики** — это главный смысл слоя (драфты новостей и неопубликованные
  страницы не должны утекать).

---

## 2. Обратная связь с публичного сайта

**Новый эндпоинт не нужен.** Уже есть анонимный
`POST /api/organizations/{organizationId}/feedbacks`.

Фронт-флоу: сначала `GET /api/public/{domain}` → получает `id`, затем `POST` на
существующий эндпоинт. (Опционально, для симметрии, можно добавить
`POST /api/public/{domain}/feedback` как тонкую обёртку — не обязательно.)

---

## 3. Мелочи и баги

- **`PagesController.GetBySlug`** (`GET .../pages/slug/{slug}`) — тело закомментировано,
  всегда возвращает `NotFound`. Либо реализовать (раскомментировать), либо удалить —
  публичный аналог из п.1 его заменяет.
- (Опц.) Rate-limit на публичные GET, чтобы контент не выкачивали ботами.

---

## 4. Для будущей «панели платформы» (этап позже, не сейчас)

Это для админки **владельца Favilonia** (список всех учреждений, тарифы) — отдельный этап.
Сейчас бэкенд к ней не готов:

- `Roles.All` содержит только `Admin` и `User`. Политика `SuperAdmin`
  (`OrganizationsController.GetAll`) **недостижима** — роли `SuperAdmin` нет.
  Нужно: добавить `Roles.SuperAdmin = "SuperAdmin"` и включить её в `All`.
- Нужен способ назначить кому-то `SuperAdmin` (сид при старте / миграция / ручной апдейт).
- Тарифы/биллинг: в текущих сущностях нет полей плана подписки — это отдельная модель.

---

## 5. Что НЕ требует изменений

- **CORS** — `Program.cs` уже `AllowAnyOrigin/AnyMethod/AnyHeader` (`FrontendPolicy`).
  Менять не нужно (фронт использует Bearer-токен, не куки).

---

## Контракты ответов (чтобы фронт совпал один-в-один)

`GET /api/public/{domain}`
```json
{ "id": "guid", "name": "Колледж №1" }
```

`GET /api/public/{domain}/news` → как существующий `NewsResponse`:
```json
{
  "items": [
    { "id": "guid", "organizationId": "guid", "title": "…",
      "content": "…", "publishedAt": "2026-06-13T…", "createdAt": "…" }
  ],
  "totalCount": 0, "totalPages": 0, "currentPage": 1, "pageSize": 20
}
```

`GET /api/public/{domain}/schedule` → как существующий `ScheduleResponse` в той же обёртке пагинации.

`GET /api/public/{domain}/pages`
```json
[ { "title": "О нас", "slug": "o-nas" } ]
```

`GET /api/public/{domain}/pages/{slug}` → как существующий `PageResponse`:
```json
{ "id": "guid", "organizationId": "guid", "title": "О нас", "slug": "o-nas",
  "content": "…", "isPublished": true, "createdAt": "…", "updatedAt": "…" }
```

> После реализации этих эндпоинтов фронт соберёт публичный сайт по пути `/s/{domain}`
> (главная с новостями/расписанием, страницы, форма обратной связи).
