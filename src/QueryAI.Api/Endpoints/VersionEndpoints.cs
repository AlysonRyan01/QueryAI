using System.Reflection;
using QueryAI.Api.Responses;

namespace QueryAI.Api.Endpoints;

internal static class VersionEndpoints
{
    public static readonly string Version =
        typeof(VersionEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    public static IEndpointRouteBuilder MapVersionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/version", () => TypedResults.Ok(new VersionResponse(Version)))
            .AllowAnonymous()
            .WithTags("Operação")
            .WithName("GetVersion")
            .WithSummary("Versão instalada");
        return app;
    }
}
