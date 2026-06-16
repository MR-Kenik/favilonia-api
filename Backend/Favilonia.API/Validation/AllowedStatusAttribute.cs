using System.ComponentModel.DataAnnotations;
using Favilonia.Domain.Entities;

namespace Favilonia.API.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class AllowedStatusAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string status)
            return false;

        return AttendanceStatus.All.Contains(status);
    }

    public override string FormatErrorMessage(string name)
    {
        return ErrorMessage ?? $"Статус посещаемости недопустим. Допустимые значения: {string.Join(", ", AttendanceStatus.All)}.";
    }
}
