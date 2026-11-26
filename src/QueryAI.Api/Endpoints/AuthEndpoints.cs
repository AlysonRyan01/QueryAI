using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using QueryAI.Api.Extensions;
using QueryAI.Api.Options;
using QueryAI.Api.Setup;
using QueryAI.Application.Auth.Dtos;
using QueryAI.Application.Auth.Requests;
using QueryAI.Application.Auth.Services;
using QueryAI.Domain.Common.Errors;

namespace QueryAI.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Autenticação");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.LoginPolicy)
            .WithName("Login")
            .Produces<CurrentUserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status423Locked)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization(AuthorizationSetup.AllowPendingPasswordChange)
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", GetMeAsync)
            .RequireAuthorization(AuthorizationSetup.AllowPendingPasswordChange)
            .WithName("GetCurrentUser")
            .Produces<CurrentUserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization(AuthorizationSetup.AllowPendingPasswordChange)
            .WithName("ChangePassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        AuthService auth,
        IOptions<SecurityOptions> security,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.IsHttps && !AuthenticationSetup.AllowsInsecureHttp(security.Value, environment))
        {
            return Error.Validation("https_required", "Esta instância exige HTTPS. Acesse pelo endereço seguro.")
                .ToProblem(httpContext);
        }

        var result = await auth.LoginAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem(httpContext);
        }

        await SignInAsync(httpContext, result.Value);
        return TypedResults.Ok(result.Value.User);
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext, AuthService auth, CancellationToken cancellationToken)
    {
        if (httpContext.User.GetUserId() is { } userId)
        {
            await auth.LogoutAsync(userId, cancellationToken);
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetMeAsync(HttpContext httpContext, AuthService auth, CancellationToken cancellationToken)
    {
        if (httpContext.User.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        return (await auth.GetCurrentUserAsync(userId, cancellationToken)).ToHttpResult(httpContext, TypedResults.Ok);
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        HttpContext httpContext,
        AuthService auth,
        CancellationToken cancellationToken)
    {
        if (httpContext.User.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await auth.ChangePasswordAsync(userId, request, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem(httpContext);
        }

        await SignInAsync(httpContext, result.Value);
        return TypedResults.NoContent();
    }

    private static Task SignInAsync(HttpContext httpContext, AuthenticatedSession session) =>
        httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            SessionClaims.CreatePrincipal(session.User, session.SecurityStamp),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
}
