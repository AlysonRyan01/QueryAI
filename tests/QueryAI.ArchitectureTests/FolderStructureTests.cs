using System.Text.RegularExpressions;

namespace QueryAI.ArchitectureTests;

public sealed partial class FolderStructureTests
{
    private static readonly HashSet<string> ContextTypeFolders =
    [
        "Entities", "Enums", "Errors", "Policies", "Results", "Interfaces", "Exceptions", "Models",
        "Services", "Requests", "Validations", "Dtos", "Settings", "Extensions", "Contexts",
        "Configurations", "Repositories", "Transactions", "Migrations", "HostedServices",
    ];

    private static readonly HashSet<string> ApiTypeFolders =
    [
        "Endpoints", "Responses", "Setup", "Options", "Validations", "Middlewares", "Handlers",
        "Requirements", "Extensions", "Services", "Policies",
    ];

    private static readonly string[] LayeredProjects = ["QueryAI.Domain", "QueryAI.Application", "QueryAI.Infrastructure"];

    [Fact]
    public void Layered_projects_keep_files_inside_type_folders_of_each_context()
    {
        var violations = new List<string>();
        foreach (var project in LayeredProjects)
        {
            foreach (var file in SourceFiles(project))
            {
                var parts = RelativeParts(project, file);
                if (parts.Length == 1)
                {
                    if (parts[0] != "DependencyInjection.cs")
                    {
                        violations.Add($"{project}/{string.Join('/', parts)}: only DependencyInjection.cs may sit at the project root");
                    }

                    continue;
                }

                if (parts.Length < 3 || !ContextTypeFolders.Contains(parts[^2]))
                {
                    violations.Add($"{project}/{string.Join('/', parts)}: must be in <Context>/<TypeFolder>/");
                }
            }
        }

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Api_is_organized_by_type_folders()
    {
        const string project = "QueryAI.Api";
        var violations = SourceFiles(project)
            .Select(file => RelativeParts(project, file))
            .Where(parts => parts.Length == 1 ? parts[0] != "Program.cs" : parts.Length != 2 || !ApiTypeFolders.Contains(parts[0]))
            .Select(parts => $"{project}/{string.Join('/', parts)}")
            .ToList();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Namespaces_match_folder_paths()
    {
        var violations = new List<string>();
        foreach (var project in LayeredProjects.Append("QueryAI.Api"))
        {
            foreach (var file in SourceFiles(project).Where(f => !f.Contains("/Migrations/", StringComparison.Ordinal)))
            {
                var match = NamespaceDeclaration().Match(File.ReadAllText(file));
                if (!match.Success)
                {
                    continue;
                }

                var folders = RelativeParts(project, file)[..^1];
                var expected = string.Join('.', new[] { project }.Concat(folders));
                if (match.Groups["ns"].Value != expected)
                {
                    violations.Add($"{project}/{string.Join('/', RelativeParts(project, file))}: namespace {match.Groups["ns"].Value}, expected {expected}");
                }
            }
        }

        violations.ShouldBeEmpty();
    }

    private static IEnumerable<string> SourceFiles(string project) =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot.Value, "src", project), "*.cs", SearchOption.AllDirectories)
            .Select(f => f.Replace('\\', '/'))
            .Where(f => !f.Contains("/bin/", StringComparison.Ordinal) && !f.Contains("/obj/", StringComparison.Ordinal));

    private static string[] RelativeParts(string project, string file) =>
        Path.GetRelativePath(Path.Combine(RepositoryRoot.Value, "src", project), file).Replace('\\', '/').Split('/');

    private static readonly Lazy<string> RepositoryRoot = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QueryAI.slnx")))
        {
            dir = dir.Parent;
        }

        return dir!.FullName;
    });

    [GeneratedRegex(@"^namespace\s+(?<ns>[\w.]+);", RegexOptions.Multiline)]
    private static partial Regex NamespaceDeclaration();
}
