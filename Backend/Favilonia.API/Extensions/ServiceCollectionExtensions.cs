using System.Linq;
using Favilonia.API.Authorization;
using Favilonia.API.Middleware;
using Favilonia.API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Favilonia.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, OrganizationAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.SameOrganization, policy =>
                policy.Requirements.Add(new OrganizationAuthorizationRequirement()));

            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(AuthorizationPolicies.SuperAdmin, policy =>
                policy.RequireRole(Roles.SuperAdmin));
        });

        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
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

        return services;
    }
}
