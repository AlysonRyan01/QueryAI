using System.Diagnostics;
using QueryAI.Api.Handlers;

namespace QueryAI.Api.Setup;

internal static class ProblemDetailsSetup
{
    public static IServiceCollection AddQueryAIProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
            if (!problem.Extensions.ContainsKey("code"))
            {
                problem.Extensions["code"] = DefaultCode(problem.Status ?? context.HttpContext.Response.StatusCode);
            }
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static string DefaultCode(int status) => status switch
    {
        400 => "validation_failed",
        401 => "unauthorized",
        403 => "forbidden",
        404 => "not_found",
        409 => "conflict",
        423 => "account_locked",
        429 => "rate_limited",
        _ => "unexpected_error",
    };
}
