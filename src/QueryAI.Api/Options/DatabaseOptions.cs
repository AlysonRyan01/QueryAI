using Npgsql;

namespace QueryAI.Api.Options;

internal sealed class DatabaseOptions
{
    public string? ConnectionStringOverride { get; set; }

    public string Host { get; set; } = "db";

    public int Port { get; set; } = 5432;

    public string Database { get; set; } = "queryai";

    public string Username { get; set; } = "queryai";

    public string? Password { get; set; }

    public string ConnectionString => !string.IsNullOrWhiteSpace(ConnectionStringOverride)
        ? ConnectionStringOverride
        : new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password,
            ApplicationName = "queryai-api",
        }.ConnectionString;
}
