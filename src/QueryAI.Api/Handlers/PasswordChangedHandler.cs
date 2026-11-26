using Microsoft.AspNetCore.Authorization;
using QueryAI.Api.Extensions;
using QueryAI.Api.Requirements;

namespace QueryAI.Api.Handlers;

internal sealed class PasswordChangedHandler : AuthorizationHandler<PasswordChangedRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PasswordChangedRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && !context.User.MustChangePasswordFlag())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
