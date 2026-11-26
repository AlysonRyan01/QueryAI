using Microsoft.Extensions.Options;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Operations;

public sealed class ConfigurationValidationTests(PostgresFixture postgres)
{
    [Theory]
    [InlineData("")]
    [InlineData("change-me")]
    [InlineData("short")]
    public async Task Startup_fails_naming_QUERYAI_ADMIN_PASSWORD(string value)
    {
        var ex = await StartAsync(new Dictionary<string, string?> { ["QUERYAI_ADMIN_PASSWORD"] = value });

        ex.Message.ShouldContain("QUERYAI_ADMIN_PASSWORD");
    }

    [Theory]
    [InlineData("")]
    [InlineData("CHANGE-ME")]
    [InlineData("not-an-email")]
    public async Task Startup_fails_naming_QUERYAI_ADMIN_EMAIL(string value)
    {
        var ex = await StartAsync(new Dictionary<string, string?> { ["QUERYAI_ADMIN_EMAIL"] = value });

        ex.Message.ShouldContain("QUERYAI_ADMIN_EMAIL");
    }

    [Fact]
    public async Task Startup_fails_naming_POSTGRES_PASSWORD_when_no_connection_override()
    {
        var ex = await StartAsync(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "",
            ["POSTGRES_PASSWORD"] = "change-me",
        });

        ex.Message.ShouldContain("POSTGRES_PASSWORD");
    }

    private async Task<OptionsValidationException> StartAsync(Dictionary<string, string?> settings)
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var factory = new QueryAIWebApplicationFactory(connectionString, settings);
        return Should.Throw<OptionsValidationException>(() => _ = factory.Server);
    }
}
