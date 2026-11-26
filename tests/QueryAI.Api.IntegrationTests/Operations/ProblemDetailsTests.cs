using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueryAI.Api.IntegrationTests.Infrastructure;
using QueryAI.Application.Abstractions.Security.Interfaces;

namespace QueryAI.Api.IntegrationTests.Operations;

public sealed class ProblemDetailsTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    protected override void ConfigureServices(IServiceCollection services) =>
        services.Replace(ServiceDescriptor.Singleton<IPasswordHasher, ThrowingPasswordHasher>());

    [Fact]
    public async Task Unhandled_exception_returns_500_problem_without_details()
    {
        var response = await LoginAsync(
            Factory.CreateApiClient(), QueryAIWebApplicationFactory.AdminEmail, QueryAIWebApplicationFactory.AdminPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var raw = await response.Content.ReadAsStringAsync(Ct);
        raw.ShouldNotContain(ThrowingPasswordHasher.InternalDetail);
        raw.ShouldNotContain("at QueryAI");
        var body = await ReadJsonAsync(response);
        body.GetProperty("code").GetString().ShouldBe("unexpected_error");
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrEmpty();
    }
}
