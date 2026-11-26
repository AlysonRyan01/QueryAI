using System.Text.RegularExpressions;

namespace QueryAI.ArchitectureTests;

public sealed partial class OneTypePerFileTests
{
    private static readonly string[] SourceDirectories = ["src", "tests"];

    [Fact]
    public void Every_source_file_declares_exactly_one_type_named_after_the_file()
    {
        var root = FindRepositoryRoot();
        var files = SourceDirectories
            .SelectMany(dir => Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            .Where(f => !IsExcluded(f))
            .ToList();
        files.Count.ShouldBeGreaterThan(50);

        var violations = new List<string>();
        foreach (var file in files)
        {
            var names = TypeDeclaration().Matches(File.ReadAllText(file)).Select(m => m.Groups["name"].Value).ToList();
            var expected = Path.GetFileNameWithoutExtension(file);
            if (names.Count != 1 || !MatchesFileName(names[0], expected))
            {
                violations.Add($"{Path.GetRelativePath(root, file)}: [{string.Join(", ", names)}]");
            }
        }

        violations.ShouldBeEmpty();
    }

    private static bool MatchesFileName(string typeName, string fileName) =>
        typeName == fileName || fileName == typeName + "OfT";

    private static bool IsExcluded(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal)
            || normalized.Contains("/Migrations/", StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "QueryAI.slnx")))
        {
            dir = dir.Parent;
        }

        return dir!.FullName;
    }

    [GeneratedRegex(@"^\s*(?:(?:public|internal|private|protected|file|static|sealed|abstract|partial|readonly|ref)\s+)*(?:class|record(?:\s+struct|\s+class)?|interface|enum|struct)\s+(?<name>\w+)", RegexOptions.Multiline)]
    private static partial Regex TypeDeclaration();
}
