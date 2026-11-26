using System.Net;
using Microsoft.Extensions.Options;
using QueryAI.Api.Options;
using QueryAI.Api.Validations;
using QueryAI.Application.Setup.Settings;

namespace QueryAI.Api.Setup;

internal static class OptionsRegistration
{
    public static IServiceCollection AddQueryAIOptions(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
            .Configure<IConfiguration>((o, c) =>
            {
                o.ConnectionStringOverride = c.GetConnectionString("Default");
                o.Host = c["POSTGRES_HOST"] ?? o.Host;
                o.Port = int.TryParse(c["POSTGRES_PORT"], out var port) ? port : o.Port;
                o.Database = c["POSTGRES_DB"] ?? o.Database;
                o.Username = c["POSTGRES_USER"] ?? o.Username;
                o.Password = c["POSTGRES_PASSWORD"];
            })
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        services.AddOptions<InitialAdminSettings>()
            .Configure<IConfiguration>((o, c) =>
            {
                o.Email = c["QUERYAI_ADMIN_EMAIL"]?.Trim() ?? string.Empty;
                o.Password = c["QUERYAI_ADMIN_PASSWORD"] ?? string.Empty;
                o.Name = string.IsNullOrWhiteSpace(c["QUERYAI_ADMIN_NAME"]) ? "Administrador" : c["QUERYAI_ADMIN_NAME"]!.Trim();
            })
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<InitialAdminSettings>, InitialAdminSettingsValidator>();

        services.AddOptions<SecurityOptions>()
            .Configure<IConfiguration>((o, c) =>
            {
                o.TrustedProxies = string.IsNullOrWhiteSpace(c["QUERYAI_TRUSTED_PROXIES"])
                    ? SecurityOptions.DefaultTrustedProxies
                    : c["QUERYAI_TRUSTED_PROXIES"]!;
                o.SessionIdleHours = int.TryParse(c["QUERYAI_SESSION_IDLE_HOURS"], out var hours) ? hours : o.SessionIdleHours;
                o.AllowInsecureHttp = bool.TryParse(c["QUERYAI_ALLOW_INSECURE_HTTP"], out var insecure) && insecure;
                o.LoginRateLimitPerMinute = int.TryParse(c["QUERYAI_LOGIN_RATE_LIMIT_PER_MINUTE"], out var limit) ? limit : o.LoginRateLimitPerMinute;
            })
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecurityOptions>, SecurityOptionsValidator>();

        services.AddOptions<ApiDocsOptions>()
            .Configure<IConfiguration>((o, c) =>
                o.Enabled = bool.TryParse(c["QUERYAI_ENABLE_API_DOCS"], out var enabled) && enabled);

        return services;
    }

    public static IReadOnlyList<IPNetwork> ParseNetworks(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Contains('/', StringComparison.Ordinal)
                ? IPNetwork.Parse(entry)
                : new IPNetwork(IPAddress.Parse(entry), IPAddress.Parse(entry).AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128))
            .ToList();
}
