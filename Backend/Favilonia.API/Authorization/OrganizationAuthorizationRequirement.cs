using Microsoft.AspNetCore.Authorization;

namespace Favilonia.API.Authorization;

// Маркерный класс-требование для политики SameOrganization.
// Логика проверки — в OrganizationAuthorizationHandler.
public class OrganizationAuthorizationRequirement : IAuthorizationRequirement
{
}
