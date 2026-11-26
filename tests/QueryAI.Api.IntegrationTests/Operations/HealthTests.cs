using System.Net;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Operations;

public sealed class HealthTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task Live_returns_200_anonymously()
    {
        var response = await Factory.CreateClient().GetAsync("/health/live", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("status").GetString().ShouldBe("Healthy");
    }

    [Fact]
    public async Task Ready_reports_database_check_and_503_when_database_is_unavailable()
    {
        var client = Factory.CreateClient();

        var healthy = await client.GetAsync("/health/ready", Ct);
        healthy.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadJsonAsync(healthy);
        body.GetProperty("checks").EnumerateArray()
            .ShouldContain(c => c.GetProperty("name").GetString() == "database" && c.GetProperty("status").GetString() == "Healthy");

        await Postgres.SetConnectionsAllowedAsync(Factory.ConnectionString, allowed: false);
        try
        {
            var unhealthy = await client.GetAsync("/health/ready", Ct);
            unhealthy.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            (await ReadJsonAsync(unhealthy)).GetProperty("checks").EnumerateArray()
                .ShouldContain(c => c.GetProperty("name").GetString() == "database" && c.GetProperty("status").GetString() == "Unhealthy");
        }
        finally
        {
            await Postgres.SetConnectionsAllowedAsync(Factory.ConnectionString, allowed: true);
        }
    }
}
