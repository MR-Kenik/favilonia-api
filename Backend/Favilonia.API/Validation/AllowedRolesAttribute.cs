using System.ComponentModel.DataAnnotations;
using System.Linq;
using Favilonia.API.Authorization;

namespace Favilonia.API.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class AllowedRolesAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string role)
        {
            return false;
        }

        return Roles.All.Contains(role);
    }

    public override string FormatErrorMessage(string name)
    {
        return ErrorMessage ?? $"Роль пользователя недопустима. Допустимые роли: {string.Join(", ", Roles.All)}.";
    }
}
