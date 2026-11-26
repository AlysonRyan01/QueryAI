using System.Net;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Operations;

public sealed class VersionTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task Returns_installed_version_anonymously()
    {
        var response = await Factory.CreateClient().GetAsync("/api/version", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("version").GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
