using System.Net;
using System.Net.Http.Json;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Auth;

public sealed class LoginTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task Valid_login_returns_user_and_secure_session_cookie()
    {
        var response = await LoginAsync(Factory.CreateApiClient(), QueryAIWebApplicationFactory.AdminEmail, QueryAIWebApplicationFactory.AdminPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("email").GetString().ShouldBe(QueryAIWebApplicationFactory.AdminEmail);
        body.GetProperty("role").GetString().ShouldBe("Admin");
        body.GetProperty("mustChangePassword").GetBoolean().ShouldBeTrue();

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("queryai.session=", StringComparison.Ordinal));
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain("samesite=strict", Case.Insensitive);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_identical_401()
    {
        var client = Factory.CreateApiClient();

        var wrong = await LoginAsync(client, QueryAIWebApplicationFactory.AdminEmail, "Wrong-Password-123");
        var unknown = await LoginAsync(client, "nobody@queryai.test", "Wrong-Password-123");

        wrong.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknown.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var wrongBody = await ReadJsonAsync(wrong);
        var unknownBody = await ReadJsonAsync(unknown);
        wrongBody.GetProperty("code").GetString().ShouldBe("invalid_credentials");
        unknownBody.GetProperty("code").GetString().ShouldBe("invalid_credentials");
        wrongBody.GetProperty("title").GetString().ShouldBe(unknownBody.GetProperty("title").GetString());
        wrongBody.GetProperty("traceId").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Five_failures_lock_the_account_with_423_and_retry_after()
    {
        var client = Factory.CreateApiClient();
        for (var i = 0; i < 4; i++)
        {
            (await LoginAsync(client, QueryAIWebApplicationFactory.AdminEmail, "Wrong-Password-123")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var locked = await LoginAsync(client, QueryAIWebApplicationFactory.AdminEmail, "Wrong-Password-123");
        locked.StatusCode.ShouldBe((HttpStatusCode)423);
        (await ReadProblemCodeAsync(locked)).ShouldBe("account_locked");
        locked.Headers.RetryAfter.ShouldNotBeNull();

        var correctButLocked = await LoginAsync(client, QueryAIWebApplicationFactory.AdminEmail, QueryAIWebApplicationFactory.AdminPassword);
        correctButLocked.StatusCode.ShouldBe((HttpStatusCode)423);
    }

    [Fact]
    public async Task Missing_csrf_header_is_rejected()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = QueryAIWebApplicationFactory.AdminEmail, password = QueryAIWebApplicationFactory.AdminPassword }, Json, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validation_errors_return_400_with_field_errors()
    {
        var response = await LoginAsync(Factory.CreateApiClient(), "", "");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadJsonAsync(response);
        body.GetProperty("code").GetString().ShouldBe("validation_failed");
        body.GetProperty("errors").TryGetProperty("email", out _).ShouldBeTrue();
    }
}
