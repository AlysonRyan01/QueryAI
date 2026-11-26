using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace QueryAI.Api.IntegrationTests.Infrastructure;

public sealed class QueryAIWebApplicationFactory(
    string connectionString,
    IReadOnlyDictionary<string, string?>? settings = null,
    Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@queryai.test";
    public const string AdminPassword = "Initial-Admin-Pass-1";

    public string ConnectionString => connectionString;

    public CapturingSink Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var defaults = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = connectionString,
            ["QUERYAI_ADMIN_EMAIL"] = AdminEmail,
            ["QUERYAI_ADMIN_PASSWORD"] = AdminPassword,
            ["QUERYAI_ADMIN_NAME"] = "Admin Inicial",
            ["QUERYAI_ALLOW_INSECURE_HTTP"] = "true",
            ["QUERYAI_LOGIN_RATE_LIMIT_PER_MINUTE"] = "1000",
            ["QUERYAI_LOG_LEVEL"] = "Debug",
        };
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            defaults[key] = value;
        }

        foreach (var (key, value) in defaults)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(Logs);
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, LoopbackRemoteIpStartupFilter>();
            configureServices?.Invoke(services);
        });
    }

    public HttpClient CreateApiClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Requested-With", "QueryAI");
        return client;
    }
}
