using Microsoft.Extensions.Options;
using QueryAI.Api.Options;
using Scalar.AspNetCore;

namespace QueryAI.Api.Setup;

internal static class ApiDocsSetup
{
    public static IServiceCollection AddQueryAIApiDocs(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "QueryAI API";
            document.Info.Description = "API da instância QueryAI. Autenticação por cookie de sessão; métodos que alteram estado exigem o header X-Requested-With: QueryAI.";
            return Task.CompletedTask;
        }));
        return services;
    }

    public static WebApplication MapQueryAIApiDocs(this WebApplication app)
    {
        var enabled = app.Environment.IsDevelopment()
            || app.Services.GetRequiredService<IOptions<ApiDocsOptions>>().Value.Enabled;
        if (!enabled)
        {
            return app;
        }

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous();
        return app;
    }
}
