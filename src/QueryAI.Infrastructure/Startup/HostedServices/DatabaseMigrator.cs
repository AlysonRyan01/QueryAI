using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using QueryAI.Infrastructure.Persistence.Contexts;

namespace QueryAI.Infrastructure.Startup.HostedServices;

internal sealed partial class DatabaseMigrator(IServiceScopeFactory scopeFactory, ILogger<DatabaseMigrator> logger)
    : IHostedService
{
    internal static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(60);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await WaitForDatabaseAsync(db, cancellationToken);

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            LogUpToDate(logger);
            return;
        }

        var migrationList = string.Join(", ", pending);
        LogApplying(logger, pending.Count, migrationList);
        await db.Database.MigrateAsync(cancellationToken);
        LogApplied(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WaitForDatabaseAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + MaxWait;
        var delay = TimeSpan.FromSeconds(1);
        while (true)
        {
            try
            {
                var connection = (NpgsqlConnection)db.Database.GetDbConnection();
                await connection.OpenAsync(cancellationToken);
                await connection.CloseAsync();
                return;
            }
            catch (NpgsqlException ex) when (ex.IsTransient || ex.InnerException is System.Net.Sockets.SocketException || ex is PostgresException { SqlState: "57P03" })
            {
                if (DateTimeOffset.UtcNow + delay > deadline)
                {
                    throw new InvalidOperationException(
                        $"O banco interno não ficou disponível em {MaxWait.TotalSeconds:0} segundos. Verifique se o serviço do banco está em execução e se POSTGRES_* estão corretos.",
                        ex);
                }

                LogWaiting(logger, delay.TotalSeconds, ex.Message);
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 8));
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database schema is up to date")]
    private static partial void LogUpToDate(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying {Count} migration(s): {Migrations}")]
    private static partial void LogApplying(ILogger logger, int count, string migrations);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations applied")]
    private static partial void LogApplied(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Database not available yet, retrying in {DelaySeconds} s: {Reason}")]
    private static partial void LogWaiting(ILogger logger, double delaySeconds, string reason);
}
