using System.Net;
using Microsoft.AspNetCore.Authorization;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Users;

public sealed class AdminAuthorizationTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task Analyst_gets_403_on_every_admin_endpoint()
    {
        var admin = await CreateAdminClientAsync();
        var (analyst, _) = await CreateUserClientAsync(admin);

        var adminEndpoints = EndpointEnumerator.List(Factory.Services)
            .Where(e => e.Metadata.OfType<IAuthorizeData>().Any(a => a.Policy == "AdminOnly"))
            .ToList();
        adminEndpoints.Count.ShouldBe(7);

        var failures = new List<string>();
        foreach (var endpoint in adminEndpoints)
        {
            using var request = endpoint.CreateRequest();
            var response = await analyst.SendAsync(request, Ct);
            if (response.StatusCode != HttpStatusCode.Forbidden)
            {
                failures.Add($"{endpoint.Method} {endpoint.Template} -> {(int)response.StatusCode}");
            }
        }

        failures.ShouldBeEmpty();
    }
}
