using Microsoft.AspNetCore.Authorization;
using QueryAI.Api.Handlers;
using QueryAI.Api.Requirements;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Api.Setup;

internal static class AuthorizationSetup
{
    public const string AdminOnly = "AdminOnly";
    public const string AllowPendingPasswordChange = "AllowPendingPasswordChange";

    public static IServiceCollection AddQueryAIAuthorization(this IServiceCollection services)
    {
        var standard = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PasswordChangedRequirement())
            .Build();

        services.AddSingleton<IAuthorizationHandler, PasswordChangedHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemAuthorizationResultHandler>();

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(standard)
            .SetFallbackPolicy(standard)
            .AddPolicy(AdminOnly, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PasswordChangedRequirement())
                .RequireRole(nameof(Role.Admin)))
            .AddPolicy(AllowPendingPasswordChange, policy => policy.RequireAuthenticatedUser());
        return services;
    }
}
