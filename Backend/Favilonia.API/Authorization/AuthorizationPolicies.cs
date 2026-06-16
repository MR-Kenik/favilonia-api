namespace Favilonia.API.Authorization;

public static class AuthorizationPolicies
{
    public const string SameOrganization = "SameOrganization";
    public const string AdminOnly = "AdminOnly";
    public const string SuperAdmin = "SuperAdmin";
}
