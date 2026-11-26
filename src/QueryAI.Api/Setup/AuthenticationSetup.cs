using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QueryAI.Api.Extensions;
using QueryAI.Api.Options;
using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Application.Auth.Dtos;

namespace QueryAI.Api.Setup;

internal static partial class AuthenticationSetup
{
    public const string CookieName = "queryai.session";

    public static IServiceCollection AddQueryAIAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<SecurityOptions>, IHostEnvironment>((options, security, environment) =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = AllowsInsecureHttp(security.Value, environment)
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(security.Value.SessionIdleHours);
                options.SlidingExpiration = true;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = ctx => WriteProblemAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized, "unauthorized", "Autenticação necessária."),
                    OnRedirectToAccessDenied = ctx => WriteProblemAsync(ctx.HttpContext, StatusCodes.Status403Forbidden, "forbidden", "Você não tem permissão para acessar este recurso."),
                    OnValidatePrincipal = ValidatePrincipalAsync,
                };
            });

        return services;
    }

    public static bool AllowsInsecureHttp(SecurityOptions security, IHostEnvironment environment) =>
        security.AllowInsecureHttp || environment.IsDevelopment();

    public static void WarnIfInsecure(WebApplication app)
    {
        var security = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value;
        if (security.AllowInsecureHttp)
        {
            LogInsecure(app.Logger);
        }
    }

    private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var userId = principal?.GetUserId();
        var stamp = principal?.FindFirst(SessionClaims.SecurityStamp)?.Value;
        if (userId is null || stamp is null)
        {
            await RejectAsync(context);
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await users.GetByIdAsync(userId.Value, context.HttpContext.RequestAborted);
        if (user is null || !user.IsActive || !string.Equals(user.SecurityStamp.ToString(), stamp, StringComparison.Ordinal))
        {
            await RejectAsync(context);
            return;
        }

        var current = SessionClaims.CreatePrincipal(CurrentUserDto.From(user), user.SecurityStamp);
        var changed =
            principal!.FindFirst(SessionClaims.Role)?.Value != user.Role.ToString()
            || principal.MustChangePasswordFlag() != user.MustChangePassword
            || principal.Identity?.Name != user.Name;
        if (changed)
        {
            context.ReplacePrincipal(current);
            context.ShouldRenew = true;
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static async Task WriteProblemAsync(HttpContext httpContext, int status, string code, string title)
    {
        httpContext.Response.StatusCode = status;
        var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Extensions = { ["code"] = code } },
        });
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "QUERYAI_ALLOW_INSECURE_HTTP=true: login over plain HTTP is allowed. Use only for local evaluation.")]
    private static partial void LogInsecure(ILogger logger);
}
