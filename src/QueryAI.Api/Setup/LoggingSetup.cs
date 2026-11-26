using System.Security.Claims;
using QueryAI.Api.Policies;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace QueryAI.Api.Setup;

internal static class LoggingSetup
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var level = Enum.TryParse<LogEventLevel>(builder.Configuration["QUERYAI_LOG_LEVEL"], ignoreCase: true, out var parsed)
            ? parsed
            : LogEventLevel.Information;

        builder.Services.AddSerilog((services, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .MinimumLevel.Is(level)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Migrations", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "QueryAI.Api")
            .Destructure.With<SensitiveDataPolicy>()
            .WriteTo.Console(new RenderedCompactJsonFormatter()),
            preserveStaticLogger: true);
    }

    public static IApplicationBuilder UseQueryAIRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
            options.GetLevel = (httpContext, _, exception) =>
                exception is not null || httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error
                : httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ? LogEventLevel.Verbose
                : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                var userId = httpContext.User.FindFirstValue("sub");
                if (userId is not null)
                {
                    diagnosticContext.Set("UserId", userId);
                }
            };
        });
}
