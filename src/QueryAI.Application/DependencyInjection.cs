using Microsoft.Extensions.DependencyInjection;
using QueryAI.Application.Auth.Services;
using QueryAI.Application.Setup.Services;
using QueryAI.Application.Users.Services;

namespace QueryAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<InitialAdminService>();
        return services;
    }
}
