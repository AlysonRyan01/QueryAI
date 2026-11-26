using QueryAI.Domain.Common.Enums;

namespace QueryAI.Domain.Common.Errors;

public sealed record Error(string Code, string Message, ErrorKind Kind)
{
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }

    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? fieldErrors = null) =>
        new(code, message, ErrorKind.Validation) { FieldErrors = fieldErrors };

    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorKind.Unauthorized);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorKind.Forbidden);
}
