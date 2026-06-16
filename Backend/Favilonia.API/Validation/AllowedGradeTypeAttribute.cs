using System.ComponentModel.DataAnnotations;
using Favilonia.Domain.Entities;

namespace Favilonia.API.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class AllowedGradeTypeAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string type)
            return false;

        return GradeType.All.Contains(type);
    }

    public override string FormatErrorMessage(string name)
    {
        return ErrorMessage ?? $"Тип оценки недопустим. Допустимые значения: {string.Join(", ", GradeType.All)}.";
    }
}
