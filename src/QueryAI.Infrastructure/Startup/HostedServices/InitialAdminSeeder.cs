using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueryAI.Application.Setup.Enums;
using QueryAI.Application.Setup.Services;
using QueryAI.Application.Setup.Settings;

namespace QueryAI.Infrastructure.Startup.HostedServices;

internal sealed partial class InitialAdminSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<InitialAdminSettings> settings,
    ILogger<InitialAdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var initialAdmin = scope.ServiceProvider.GetRequiredService<InitialAdminService>();
        var outcome = await initialAdmin.EnsureAsync(settings.Value, cancellationToken);

        if (outcome == InitialAdminOutcome.Created)
        {
            LogCreated(logger);
        }
        else
        {
            LogExists(logger);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Initial administrator created; password change required on first login")]
    private static partial void LogCreated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Initial administrator already exists; no changes made")]
    private static partial void LogExists(ILogger logger);
}
