using System.Collections.Generic;

namespace Favilonia.API.Responses;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int Status { get; set; }
    public object? Details { get; set; }
}

public class ValidationErrorResponse : ErrorResponse
{
    public IReadOnlyCollection<ValidationError> Errors { get; set; } = Array.Empty<ValidationError>();
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
