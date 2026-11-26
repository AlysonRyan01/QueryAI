using System.Net;
using System.Net.Http.Json;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Users;

public sealed class LastAdminTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task Cannot_deactivate_or_demote_the_only_active_admin()
    {
        var admin = await CreateAdminClientAsync();
        var me = await ReadJsonAsync(await admin.GetAsync("/api/auth/me", Ct));
        var id = me.GetProperty("id").GetGuid();

        var deactivate = await admin.PostAsync($"/api/users/{id}/deactivate", null, Ct);
        deactivate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadProblemCodeAsync(deactivate)).ShouldBe("last_admin");

        var demote = await admin.PutAsJsonAsync($"/api/users/{id}", new { name = "Admin", role = "Analyst" }, Json, Ct);
        demote.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadProblemCodeAsync(demote)).ShouldBe("last_admin");
    }

    [Fact]
    public async Task Concurrent_mutual_deactivation_of_two_admins_leaves_one_active()
    {
        var adminA = await CreateAdminClientAsync();
        var idA = (await ReadJsonAsync(await adminA.GetAsync("/api/auth/me", Ct))).GetProperty("id").GetGuid();
        var (adminB, idB) = await CreateUserClientAsync(adminA, role: "Admin");

        var results = await Task.WhenAll(
            adminA.PostAsync($"/api/users/{idB}/deactivate", null, Ct),
            adminB.PostAsync($"/api/users/{idA}/deactivate", null, Ct));

        var statuses = results.Select(r => r.StatusCode).ToList();
        statuses.Count(s => s == HttpStatusCode.NoContent).ShouldBeLessThanOrEqualTo(1);

        var list = await ReadJsonAsync(await (await CreateFreshAdminSessionAsync(adminA, adminB)).GetAsync("/api/users", Ct));
        list.GetProperty("items").EnumerateArray()
            .Count(u => u.GetProperty("role").GetString() == "Admin" && u.GetProperty("isActive").GetBoolean())
            .ShouldBe(1);
    }

    private static async Task<HttpClient> CreateFreshAdminSessionAsync(params HttpClient[] candidates)
    {
        foreach (var client in candidates)
        {
            if ((await client.GetAsync("/api/users", Ct)).StatusCode == HttpStatusCode.OK)
            {
                return client;
            }
        }

        throw new InvalidOperationException("No admin session remained valid.");
    }
}
