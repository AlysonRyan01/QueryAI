using FluentValidation;
using QueryAI.Domain.Common.Errors;

namespace QueryAI.Application.Common.Extensions;

internal static class RequestValidation
{
    public static async Task<Error?> ValidateToErrorAsync<TRequest>(
        this IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return null;
        }

        var fieldErrors = result.Errors
            .GroupBy(f => ToCamelCase(f.PropertyName), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray(), StringComparer.Ordinal);

        return Error.Validation("validation_failed", "Os dados informados são inválidos.", fieldErrors);
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
