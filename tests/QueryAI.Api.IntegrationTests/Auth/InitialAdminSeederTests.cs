using Npgsql;
using QueryAI.Api.IntegrationTests.Infrastructure;
using QueryAI.Api.IntegrationTests.Operations;

namespace QueryAI.Api.IntegrationTests.Auth;

public sealed class InitialAdminSeederTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task First_start_creates_admin_requiring_password_change_and_audits()
    {
        var cs = await postgres.CreateDatabaseAsync();
        await using (var factory = new QueryAIWebApplicationFactory(cs))
        {
            _ = factory.Server;
        }

        var (role, mustChange) = await ReadAdminAsync(cs, QueryAIWebApplicationFactory.AdminEmail);
        role.ShouldBe("Admin");
        mustChange.ShouldBeTrue();
        (await ScalarAsync(cs, "SELECT COUNT(*) FROM security_audit_events WHERE event_type = 'InitialAdminCreated'")).ShouldBe(1L);
    }

    [Fact]
    public async Task Restart_with_different_password_does_not_overwrite_existing_admin()
    {
        var cs = await postgres.CreateDatabaseAsync();
        await using (var factory = new QueryAIWebApplicationFactory(cs))
        {
            _ = factory.Server;
        }

        var hashBefore = await ScalarAsync(cs, "SELECT password_hash FROM users");

        await using (var factory = new QueryAIWebApplicationFactory(
            cs, new Dictionary<string, string?> { ["QUERYAI_ADMIN_PASSWORD"] = "Another-Password-99" }))
        {
            _ = factory.Server;
        }

        (await MigrationTests.CountAsync(cs, "users")).ShouldBe(1);
        (await ScalarAsync(cs, "SELECT password_hash FROM users")).ShouldBe(hashBefore);
    }

    [Fact]
    public async Task Changed_admin_email_creates_a_second_admin_and_keeps_the_first()
    {
        var cs = await postgres.CreateDatabaseAsync();
        await using (var factory = new QueryAIWebApplicationFactory(cs))
        {
            _ = factory.Server;
        }

        await using (var factory = new QueryAIWebApplicationFactory(
            cs, new Dictionary<string, string?> { ["QUERYAI_ADMIN_EMAIL"] = "novo.admin@queryai.test" }))
        {
            _ = factory.Server;
        }

        (await MigrationTests.CountAsync(cs, "users")).ShouldBe(2);
        (await ReadAdminAsync(cs, QueryAIWebApplicationFactory.AdminEmail)).Role.ShouldBe("Admin");
        (await ReadAdminAsync(cs, "novo.admin@queryai.test")).Role.ShouldBe("Admin");
    }

    private static async Task<(string Role, bool MustChange)> ReadAdminAsync(string cs, string email)
    {
        await using var connection = new NpgsqlConnection(cs);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(
            "SELECT role, must_change_password FROM users WHERE normalized_email = upper(@e)", connection);
        command.Parameters.AddWithValue("e", email);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        (await reader.ReadAsync(Ct)).ShouldBeTrue($"user {email} not found");
        return (reader.GetString(0), reader.GetBoolean(1));
    }

    private static async Task<object?> ScalarAsync(string cs, string sql)
    {
        await using var connection = new NpgsqlConnection(cs);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync(Ct);
    }
}
