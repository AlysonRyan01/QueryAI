using System.Net;
using System.Net.Http.Json;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Auth;

public sealed class HttpsRequiredTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    protected override IReadOnlyDictionary<string, string?> Settings =>
        new Dictionary<string, string?> { ["QUERYAI_ALLOW_INSECURE_HTTP"] = "false" };

    [Fact]
    public async Task Login_over_plain_http_returns_https_required()
    {
        var response = await LoginAsync(Factory.CreateApiClient(), QueryAIWebApplicationFactory.AdminEmail, QueryAIWebApplicationFactory.AdminPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadProblemCodeAsync(response)).ShouldBe("https_required");
    }

    [Fact]
    public async Task Login_forwarded_as_https_by_trusted_proxy_succeeds_with_secure_cookie()
    {
        var client = Factory.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(
                new { email = QueryAIWebApplicationFactory.AdminEmail, password = QueryAIWebApplicationFactory.AdminPassword }),
        };
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("queryai.session=", StringComparison.Ordinal))
            .ShouldContain("secure", Case.Insensitive);
    }
}
