using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using QueryAI.Api.Endpoints;
using QueryAI.Api.Middlewares;
using QueryAI.Api.Options;
using QueryAI.Api.Services;
using QueryAI.Api.Setup;
using QueryAI.Application;
using QueryAI.Application.Abstractions.Security.Interfaces;
using QueryAI.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter())
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigureSerilog();

    builder.Services.AddQueryAIOptions();
    builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString);

    builder.Services.AddQueryAIProblemDetails();
    builder.Services.AddQueryAIHealthChecks();
    builder.Services.AddQueryAIForwardedHeaders();
    builder.Services.AddQueryAIAuthentication();
    builder.Services.AddQueryAIAuthorization();
    builder.Services.AddQueryAIRateLimiting();
    builder.Services.AddQueryAIApiDocs();

    var app = builder.Build();

    AuthenticationSetup.WarnIfInsecure(app);

    app.UseForwardedHeaders();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseQueryAIRequestLogging();
    app.UseRouting();
    app.UseMiddleware<CsrfHeaderMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapQueryAIHealthChecks();
    app.MapVersionEndpoints();
    app.MapAuthEndpoints();
    app.MapUserEndpoints();
    app.MapQueryAIApiDocs();

    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "QueryAI API failed to start");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
