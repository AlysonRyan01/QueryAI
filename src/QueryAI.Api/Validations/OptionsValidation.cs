namespace QueryAI.Api.Validations;

internal static class OptionsValidation
{
    private static readonly string[] Placeholders = ["change-me", "changeme", "change_me", "example", "<required>"];

    public static bool IsMissingOrPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || Placeholders.Any(p => value.Trim().Equals(p, StringComparison.OrdinalIgnoreCase));

    public static void Require(ICollection<string> failures, string? value, string environmentVariable)
    {
        if (IsMissingOrPlaceholder(value))
        {
            failures.Add($"A variável de ambiente {environmentVariable} é obrigatória e não pode estar vazia nem conter um valor de exemplo.");
        }
    }
}
