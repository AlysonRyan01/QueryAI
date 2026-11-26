using Microsoft.AspNetCore.Mvc;

namespace QueryAI.Api.Middlewares;

internal sealed class CsrfHeaderMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "QueryAI";

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Request;
        var isUnsafe = HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsPatch(request.Method) || HttpMethods.IsDelete(request.Method);

        if (isUnsafe
            && request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Headers[HeaderName], HeaderValue, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await problemDetails.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = $"O header {HeaderName}: {HeaderValue} é obrigatório.",
                    Extensions = { ["code"] = "validation_failed" },
                },
            });
            return;
        }

        await next(context);
    }
}
