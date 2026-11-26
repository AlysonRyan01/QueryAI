using System.Net;
using System.Net.Http.Json;
using Npgsql;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Users;

public sealed class UserManagementTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task Admin_creates_user_who_must_change_password()
    {
        var admin = await CreateAdminClientAsync();

        var created = await admin.PostAsJsonAsync("/api/users",
            new { name = "Bruno Costa", email = "Bruno@QueryAI.test", role = "Analyst", temporaryPassword = "Temporary-Pass-123" }, Json, Ct);

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadJsonAsync(created);
        var id = body.GetProperty("id").GetGuid();
        created.Headers.Location!.ToString().ShouldBe($"/api/users/{id}");
        body.GetProperty("mustChangePassword").GetBoolean().ShouldBeTrue();
        body.GetProperty("role").GetString().ShouldBe("Analyst");
        body.TryGetProperty("passwordHash", out _).ShouldBeFalse();

        var user = Factory.CreateApiClient();
        (await LoginAsync(user, "bruno@queryai.test", "Temporary-Pass-123")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadProblemCodeAsync(await user.GetAsync("/api/users", Ct))).ShouldBe("forbidden");
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_case_insensitively()
    {
        var admin = await CreateAdminClientAsync();
        var payload = new { name = "Carla", email = "carla@queryai.test", role = "Analyst", temporaryPassword = "Temporary-Pass-123" };
        (await admin.PostAsJsonAsync("/api/users", payload, Json, Ct)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await admin.PostAsJsonAsync("/api/users", payload with { email = "CARLA@queryai.test" }, Json, Ct);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadProblemCodeAsync(duplicate)).ShouldBe("email_already_exists");
    }

    [Theory]
    [InlineData("B", "b@queryai.test", "Temporary-Pass-123", "name")]
    [InlineData("Bruno", "invalid", "Temporary-Pass-123", "email")]
    [InlineData("Bruno", "b@queryai.test", "short", "temporaryPassword")]
    public async Task Invalid_input_returns_field_errors(string name, string email, string password, string field)
    {
        var admin = await CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/users", new { name, email, role = "Analyst", temporaryPassword = password }, Json, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty(field, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task List_supports_search_and_pagination()
    {
        var admin = await CreateAdminClientAsync();
        foreach (var name in new[] { "Alice Souza", "Alberto Lima", "Zeca Pereira" })
        {
            await admin.PostAsJsonAsync("/api/users",
                new { name, email = $"{name.Split(' ')[0].ToLowerInvariant()}@queryai.test", role = "Analyst", temporaryPassword = "Temporary-Pass-123" }, Json, Ct);
        }

        var search = await ReadJsonAsync(await admin.GetAsync("/api/users?search=al", Ct));
        search.GetProperty("total").GetInt32().ShouldBe(2);

        var page = await ReadJsonAsync(await admin.GetAsync("/api/users?page=2&pageSize=2", Ct));
        page.GetProperty("total").GetInt32().ShouldBe(4);
        page.GetProperty("items").GetArrayLength().ShouldBe(2);

        (await admin.GetAsync("/api/users?pageSize=101", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_unknown_user_returns_404()
    {
        var admin = await CreateAdminClientAsync();

        var response = await admin.GetAsync($"/api/users/{Guid.NewGuid()}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadProblemCodeAsync(response)).ShouldBe("not_found");
    }

    [Fact]
    public async Task Update_reset_deactivate_and_activate_flow_with_audit()
    {
        var admin = await CreateAdminClientAsync();
        var (userClient, id) = await CreateUserClientAsync(admin);

        var updated = await admin.PutAsJsonAsync($"/api/users/{id}", new { name = "Nome Novo", role = "Admin" }, Json, Ct);
        updated.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync(updated)).GetProperty("role").GetString().ShouldBe("Admin");

        (await userClient.GetAsync("/api/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await admin.PostAsJsonAsync($"/api/users/{id}/reset-password", new { temporaryPassword = "Reset-Temp-Pass-1" }, Json, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var relogin = Factory.CreateApiClient();
        var login = await LoginAsync(relogin, (await ReadJsonAsync(updated)).GetProperty("email").GetString()!, "Reset-Temp-Pass-1");
        (await ReadJsonAsync(login)).GetProperty("mustChangePassword").GetBoolean().ShouldBeTrue();

        (await admin.PostAsync($"/api/users/{id}/deactivate", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await relogin.GetAsync("/api/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await admin.PostAsync($"/api/users/{id}/activate", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadJsonAsync(await admin.GetAsync($"/api/users/{id}", Ct))).GetProperty("isActive").GetBoolean().ShouldBeTrue();

        var events = await ReadAuditEventsAsync(id);
        events.ShouldContain("UserCreated");
        events.ShouldContain("UserUpdated");
        events.ShouldContain("UserRoleChanged");
        events.ShouldContain("PasswordReset");
        events.ShouldContain("UserDeactivated");
        events.ShouldContain("UserActivated");
    }

    private async Task<List<string>> ReadAuditEventsAsync(Guid targetId)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand("SELECT event_type FROM security_audit_events WHERE target_user_id = @id", connection);
        command.Parameters.AddWithValue("id", targetId);
        var events = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(Ct);
        while (await reader.ReadAsync(Ct))
        {
            events.Add(reader.GetString(0));
        }

        return events;
    }
}
