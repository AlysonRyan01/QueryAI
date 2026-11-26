using Microsoft.AspNetCore.Mvc;
using QueryAI.Domain.Common.Enums;
using QueryAI.Domain.Common.Errors;
using QueryAI.Domain.Common.Results;

namespace QueryAI.Api.Extensions;

internal static class ResultExtensions
{
    public static IResult ToProblem(this Error error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        var status = error.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Locked => StatusCodes.Status423Locked,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Message,
            Type = $"https://queryai.local/problems/{error.Code}",
            Extensions = { ["code"] = error.Code },
        };

        if (error.FieldErrors is { Count: > 0 })
        {
            problem.Extensions["errors"] = error.FieldErrors;
        }

        if (error.Metadata?.TryGetValue("retryAfterSeconds", out var retryAfter) == true)
        {
            httpContext.Response.Headers.RetryAfter = retryAfter.ToString();
            problem.Extensions["retryAfterSeconds"] = retryAfter;
        }

        return Results.Problem(problem);
    }

    public static IResult ToHttpResult(this Result result, HttpContext httpContext, Func<IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        return result.IsSuccess ? onSuccess() : result.Error.ToProblem(httpContext);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext, Func<T, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        return result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem(httpContext);
    }
}
