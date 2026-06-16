using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Favilonia.API.Authorization;

// Главный изолирующий барьер мультитенантности.
// Проверяет, что organizationId из JWT-токена совпадает с {organizationId} в маршруте.
// Если они расходятся — пользователь получит 403 ещё до входа в контроллер.
// Применяется через политику SameOrganization на всех тенант-скопед контроллерах.
public class OrganizationAuthorizationHandler : AuthorizationHandler<OrganizationAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OrganizationAuthorizationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var organizationClaim = context.User.FindFirst("organizationId")?.Value;
        if (!Guid.TryParse(organizationClaim, out var userOrganizationId))
        {
            return Task.CompletedTask;
        }

        if (context.Resource is not AuthorizationFilterContext authorizationContext)
        {
            return Task.CompletedTask;
        }

        if (!authorizationContext.RouteData.Values.TryGetValue("organizationId", out var routeValue))
        {
            return Task.CompletedTask;
        }

        if (!Guid.TryParse(routeValue?.ToString(), out var routeOrganizationId))
        {
            return Task.CompletedTask;
        }

        if (routeOrganizationId == userOrganizationId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
