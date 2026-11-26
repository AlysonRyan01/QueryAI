using System.Net.Http.Json;
using Npgsql;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Security;

public sealed class SecretLeakTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task Passwords_hashes_and_db_secrets_never_leak()
    {
        const string adminNew = "Admin-Secret-Changed-77";
        const string userTemp = "User-Temporary-Secret-88";
        const string userReset = "User-Reset-Secret-99";
        var responses = new List<string>();

        var admin = Factory.CreateApiClient();
        var r1 = await LoginAsync(admin, QueryAIWebApplicationFactory.AdminEmail, QueryAIWebApplicationFactory.AdminPassword);
        responses.Add(await r1.Content.ReadAsStringAsync(Ct));
        responses.Add(await (await LoginAsync(Factory.CreateApiClient(), QueryAIWebApplicationFactory.AdminEmail, "Wrong-Secret-Attempt-1")).Content.ReadAsStringAsync(Ct));
        responses.Add(await (await admin.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = QueryAIWebApplicationFactory.AdminPassword, newPassword = adminNew }, Json, Ct)).Content.ReadAsStringAsync(Ct));

        var created = await admin.PostAsJsonAsync("/api/users",
            new { name = "Segredo Teste", email = "secret@queryai.test", role = "Analyst", temporaryPassword = userTemp }, Json, Ct);
        responses.Add(await created.Content.ReadAsStringAsync(Ct));
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        responses.Add(await (await admin.PostAsJsonAsync($"/api/users/{id}/reset-password", new { temporaryPassword = userReset }, Json, Ct)).Content.ReadAsStringAsync(Ct));
        responses.Add(await (await admin.GetAsync("/api/users", Ct)).Content.ReadAsStringAsync(Ct));
        responses.Add(await (await admin.GetAsync($"/api/users/{id}", Ct)).Content.ReadAsStringAsync(Ct));

        var hashes = await ReadHashesAsync();
        var dbPassword = new NpgsqlConnectionStringBuilder(Postgres.ServerConnectionString).Password;
        var secrets = new List<string>
        {
            QueryAIWebApplicationFactory.AdminPassword, adminNew, userTemp, userReset, "Wrong-Secret-Attempt-1",
        };
        secrets.AddRange(hashes);
        if (!string.IsNullOrEmpty(dbPassword))
        {
            secrets.Add(dbPassword);
        }

        var haystacks = new Dictionary<string, string>
        {
            ["logs"] = Factory.Logs.AllText,
            ["responses"] = string.Join('\n', responses),
        };
        Factory.Logs.Lines.Count.ShouldBeGreaterThan(0);

        foreach (var (where, text) in haystacks)
        {
            foreach (var secret in secrets.Where(s => s.Length >= 8))
            {
                text.Contains(secret, StringComparison.Ordinal).ShouldBeFalse($"secret leaked in {where}");
            }
        }
    }

    private async Task<List<string>> ReadHashesAsync()
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand("SELECT password_hash FROM users", connection);
        var hashes = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(Ct);
        while (await reader.ReadAsync(Ct))
        {
            hashes.Add(reader.GetString(0));
        }

        return hashes;
    }
}
