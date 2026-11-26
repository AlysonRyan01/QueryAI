using Npgsql;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Operations;

public sealed class MigrationTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Restarting_is_idempotent_and_preserves_data()
    {
        var connectionString = await postgres.CreateDatabaseAsync();

        await using (var first = new QueryAIWebApplicationFactory(connectionString))
        {
            _ = first.Server;
        }

        var historyBefore = await CountAsync(connectionString, "__ef_migrations_history");
        var usersBefore = await CountAsync(connectionString, "users");
        usersBefore.ShouldBe(1);

        await using (var second = new QueryAIWebApplicationFactory(connectionString))
        {
            _ = second.Server;
        }

        (await CountAsync(connectionString, "__ef_migrations_history")).ShouldBe(historyBefore);
        (await CountAsync(connectionString, "users")).ShouldBe(usersBefore);
    }

    [Fact]
    public async Task Concurrent_startups_both_succeed_with_a_single_admin()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var a = new QueryAIWebApplicationFactory(connectionString);
        await using var b = new QueryAIWebApplicationFactory(connectionString);

        await Task.WhenAll(Task.Run(() => a.Server, Ct), Task.Run(() => b.Server, Ct));

        (await CountAsync(connectionString, "users")).ShouldBe(1);
    }

    internal static async Task<long> CountAsync(string connectionString, string table)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {table}", connection);
        return (long)(await command.ExecuteScalarAsync(Ct))!;
    }
}
