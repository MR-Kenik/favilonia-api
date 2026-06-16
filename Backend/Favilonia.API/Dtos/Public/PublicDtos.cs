namespace Favilonia.API.Dtos.Public;

public class OrganizationInfoResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PublicPageListItemResponse
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
