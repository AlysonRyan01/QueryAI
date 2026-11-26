using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace QueryAI.Api.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase(PostgresFixture postgres) : IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected PostgresFixture Postgres { get; } = postgres;

    protected QueryAIWebApplicationFactory Factory { get; private set; } = null!;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected virtual IReadOnlyDictionary<string, string?>? Settings => null;

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public virtual async ValueTask InitializeAsync()
    {
        Factory = new QueryAIWebApplicationFactory(await Postgres.CreateDatabaseAsync(), Settings, ConfigureServices);
        _ = Factory.Server;
    }

    public virtual async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password }, Json);

    protected async Task<HttpClient> CreateAdminClientAsync(string newPassword = "Admin-Changed-Pass-1")
    {
        var client = Factory.CreateApiClient();
        (await LoginAsync(client, QueryAIWebApplicationFactory.AdminEmail, QueryAIWebApplicationFactory.AdminPassword))
            .EnsureSuccessStatusCode();
        var change = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = QueryAIWebApplicationFactory.AdminPassword, newPassword },
            Json,
            Ct);
        change.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        return client;
    }

    protected async Task<(HttpClient Client, Guid Id)> CreateUserClientAsync(
        HttpClient admin, string role = "Analyst", string? email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@queryai.test";
        const string temporary = "Temporary-Pass-123";
        var created = await admin.PostAsJsonAsync(
            "/api/users", new { name = "Usuário Teste", email, role, temporaryPassword = temporary }, Json, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(Json, Ct)).GetProperty("id").GetGuid();

        var client = Factory.CreateApiClient();
        (await LoginAsync(client, email, temporary)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync(
            "/api/auth/change-password", new { currentPassword = temporary, newPassword = "User-Changed-Pass-1" }, Json, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        return (client, id);
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json, Ct);

    protected static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await ReadJsonAsync(response);
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
