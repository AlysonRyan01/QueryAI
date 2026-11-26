using Microsoft.Extensions.Options;
using QueryAI.Api.Options;

namespace QueryAI.Api.Validations;

internal sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionStringOverride))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        OptionsValidation.Require(failures, options.Password, "POSTGRES_PASSWORD");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
