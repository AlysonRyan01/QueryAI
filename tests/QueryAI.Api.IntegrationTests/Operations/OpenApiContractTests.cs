using System.Text.RegularExpressions;
using QueryAI.Api.IntegrationTests.Infrastructure;

namespace QueryAI.Api.IntegrationTests.Operations;

public sealed partial class OpenApiContractTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    protected override IReadOnlyDictionary<string, string?> Settings =>
        new Dictionary<string, string?> { ["QUERYAI_ENABLE_API_DOCS"] = "true" };

    [Fact]
    public async Task Generated_document_contains_all_contract_operations()
    {
        var contract = ReadContractOperations();
        contract.Count.ShouldBeGreaterThan(10);

        var document = await ReadJsonAsync(await Factory.CreateClient().GetAsync("/openapi/v1.json", Ct));
        var generated = document.GetProperty("paths").EnumerateObject()
            .SelectMany(p => p.Value.EnumerateObject().Select(m => $"{m.Name.ToUpperInvariant()} {p.Name}"))
            .ToHashSet(StringComparer.Ordinal);

        var missing = contract.Where(op => !op.StartsWith("GET /health", StringComparison.Ordinal) && !generated.Contains(op)).ToList();
        missing.ShouldBeEmpty();
    }

    private static List<string> ReadContractOperations()
    {
        var path = FindContract();
        var operations = new List<string>();
        string? currentPath = null;
        foreach (var line in File.ReadLines(path))
        {
            var pathMatch = PathLine().Match(line);
            if (pathMatch.Success)
            {
                currentPath = pathMatch.Groups[1].Value;
                continue;
            }

            var methodMatch = MethodLine().Match(line);
            if (methodMatch.Success && currentPath is not null)
            {
                operations.Add($"{methodMatch.Groups[1].Value.ToUpperInvariant()} {currentPath}");
            }
        }

        return operations;
    }

    private static string FindContract()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QueryAI.slnx")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir!.FullName, "specs", "001-project-foundation", "contracts", "openapi.yaml");
    }

    [GeneratedRegex(@"^  (/[^:]+):\s*$")]
    private static partial Regex PathLine();

    [GeneratedRegex(@"^    (get|post|put|delete|patch):\s*$")]
    private static partial Regex MethodLine();
}
