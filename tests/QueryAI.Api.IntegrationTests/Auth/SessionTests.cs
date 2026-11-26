using System.Net;
using System.Net.Http.Json;
using Npgsql;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Auth;

public sealed class SessionTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task Me_requires_authentication()
    {
        var response = await Factory.CreateApiClient().GetAsync("/api/auth/me", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadProblemCodeAsync(response)).ShouldBe("unauthorized");
    }

    [Fact]
    public async Task Must_change_password_blocks_protected_routes_until_changed()
    {
        var client = Factory.CreateApiClient();
        (await LoginAsync(client, QueryAIWebApplicationFactory.AdminEmail, QueryAIWebApplicationFactory.AdminPassword)).EnsureSuccessStatusCode();

        (await client.GetAsync("/api/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var blocked = await client.GetAsync("/api/users", Ct);
        blocked.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadProblemCodeAsync(blocked)).ShouldBe("password_change_required");

        var wrongCurrent = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "Not-The-Password-1", newPassword = "New-Admin-Pass-123" }, Json, Ct);
        wrongCurrent.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadProblemCodeAsync(wrongCurrent)).ShouldBe("invalid_current_password");

        var same = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = QueryAIWebApplicationFactory.AdminPassword, newPassword = QueryAIWebApplicationFactory.AdminPassword }, Json, Ct);
        (await ReadProblemCodeAsync(same)).ShouldBe("same_password");

        var tooShort = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = QueryAIWebApplicationFactory.AdminPassword, newPassword = "short" }, Json, Ct);
        tooShort.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var changed = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = QueryAIWebApplicationFactory.AdminPassword, newPassword = "New-Admin-Pass-123" }, Json, Ct);
        changed.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync("/api/users", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync(await client.GetAsync("/api/auth/me", Ct))).GetProperty("mustChangePassword").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var client = await CreateAdminClientAsync();

        (await client.PostAsync("/api/auth/logout", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync("/api/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Rotated_security_stamp_invalidates_existing_cookie()
    {
        var client = await CreateAdminClientAsync();
        (await client.GetAsync("/api/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        await using (var connection = new NpgsqlConnection(Factory.ConnectionString))
        {
            await connection.OpenAsync(Ct);
            await using var command = new NpgsqlCommand("UPDATE users SET security_stamp = gen_random_uuid()", connection);
            await command.ExecuteNonQueryAsync(Ct);
        }

        (await client.GetAsync("/api/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
