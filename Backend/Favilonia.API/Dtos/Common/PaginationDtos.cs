namespace Favilonia.API.Dtos.Common;

/// <summary>
/// Запрос с параметрами пагинации
/// </summary>
public class PaginationRequest
{
    /// <summary>
    /// Номер страницы (начиная с 1)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Валидация параметров
    /// </summary>
    public void Validate()
    {
        if (Page < 1) Page = 1;
        if (PageSize < 1) PageSize = 20;
        if (PageSize > 100) PageSize = 100; // максимум 100 элементов на странице
    }
}

/// <summary>
/// Ответ с пагинированными данными
/// </summary>
/// <typeparam name="T">Тип элементов в списке</typeparam>
public class PaginationResponse<T>
{
    /// <summary>
    /// Список элементов текущей страницы
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Всего элементов в БД
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Количество страниц
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Текущая страница
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Есть ли следующая страница
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Есть ли предыдущая страница
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    public PaginationResponse()
    {
    }

    public PaginationResponse(List<T> items, int totalCount, int currentPage, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalPages = (totalCount + pageSize - 1) / pageSize; // округлённое деление
    }
}
