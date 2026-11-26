using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(QueryAI.Api.IntegrationTests.Infrastructure.PostgresFixture))]

namespace QueryAI.Api.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ServerConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("QUERYAI_TEST_POSTGRES");
        if (!string.IsNullOrWhiteSpace(external))
        {
            ServerConnectionString = external;
            return;
        }

        _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _container.StartAsync();
        ServerConnectionString = _container.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var name = "queryai_test_" + Guid.NewGuid().ToString("N")[..12];
        await using (var connection = new NpgsqlConnection(ServerConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = name, Pooling = false }.ConnectionString;
    }

    public async Task SetConnectionsAllowedAsync(string databaseConnectionString, bool allowed)
    {
        var database = new NpgsqlConnectionStringBuilder(databaseConnectionString).Database;
        await using var connection = new NpgsqlConnection(ServerConnectionString);
        await connection.OpenAsync();
        await using var alter = new NpgsqlCommand(
            $"ALTER DATABASE \"{database}\" WITH ALLOW_CONNECTIONS {(allowed ? "true" : "false")}", connection);
        await alter.ExecuteNonQueryAsync();
        if (!allowed)
        {
            await using var kill = new NpgsqlCommand(
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{database}' AND pid <> pg_backend_pid()",
                connection);
            await kill.ExecuteNonQueryAsync();
        }
    }
}
