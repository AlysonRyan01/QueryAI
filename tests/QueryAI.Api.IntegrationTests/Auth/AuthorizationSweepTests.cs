using System.Net;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Auth;

public sealed class AuthorizationSweepTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private static readonly string[] PublicEndpoints =
    [
        "GET /health/live", "GET /health/ready", "GET /api/version", "POST /api/auth/login",
    ];

    [Fact]
    public async Task All_non_public_endpoints_return_401_anonymously()
    {
        var endpoints = EndpointEnumerator.List(Factory.Services)
            .Where(e => !PublicEndpoints.Contains($"{e.Method} {e.Template}"))
            .Where(e => !e.Template.StartsWith("/openapi", StringComparison.Ordinal) && !e.Template.StartsWith("/scalar", StringComparison.Ordinal))
            .ToList();
        endpoints.Count.ShouldBeGreaterThan(5);

        var client = Factory.CreateApiClient();
        var failures = new List<string>();
        foreach (var endpoint in endpoints)
        {
            using var request = endpoint.CreateRequest();
            var response = await client.SendAsync(request, Ct);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                failures.Add($"{endpoint.Method} {endpoint.Template} -> {(int)response.StatusCode}");
            }
        }

        failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task Pending_password_change_blocks_every_endpoint_except_me_change_password_and_logout()
    {
        string[] allowed = [.. PublicEndpoints, "GET /api/auth/me", "POST /api/auth/change-password", "POST /api/auth/logout"];
        var endpoints = EndpointEnumerator.List(Factory.Services)
            .Where(e => !allowed.Contains($"{e.Method} {e.Template}"))
            .Where(e => !e.Template.StartsWith("/openapi", StringComparison.Ordinal) && !e.Template.StartsWith("/scalar", StringComparison.Ordinal))
            .ToList();

        var client = Factory.CreateApiClient();
        (await LoginAsync(client, QueryAIWebApplicationFactory.AdminEmail, QueryAIWebApplicationFactory.AdminPassword)).EnsureSuccessStatusCode();

        var failures = new List<string>();
        foreach (var endpoint in endpoints)
        {
            using var request = endpoint.CreateRequest();
            var response = await client.SendAsync(request, Ct);
            var code = response.StatusCode == HttpStatusCode.Forbidden ? await ReadProblemCodeAsync(response) : null;
            if (code != "password_change_required")
            {
                failures.Add($"{endpoint.Method} {endpoint.Template} -> {(int)response.StatusCode} {code}");
            }
        }

        failures.ShouldBeEmpty();
    }
}
