using System.Net;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Auth;

public sealed class LoginRateLimitTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    protected override IReadOnlyDictionary<string, string?> Settings =>
        new Dictionary<string, string?> { ["QUERYAI_LOGIN_RATE_LIMIT_PER_MINUTE"] = "3" };

    [Fact]
    public async Task Exceeding_the_per_ip_limit_returns_429()
    {
        var client = Factory.CreateApiClient();
        for (var i = 0; i < 3; i++)
        {
            await LoginAsync(client, $"x{i}@queryai.test", "Wrong-Password-123");
        }

        var limited = await LoginAsync(client, "y@queryai.test", "Wrong-Password-123");

        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await ReadProblemCodeAsync(limited)).ShouldBe("rate_limited");
    }
}
