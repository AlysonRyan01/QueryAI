using System.Net.Mail;
using Microsoft.Extensions.Options;
using QueryAI.Application.Setup.Settings;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Policies;

namespace QueryAI.Api.Validations;

internal sealed class InitialAdminSettingsValidator : IValidateOptions<InitialAdminSettings>
{
    public ValidateOptionsResult Validate(string? name, InitialAdminSettings options)
    {
        var failures = new List<string>();
        OptionsValidation.Require(failures, options.Email, "QUERYAI_ADMIN_EMAIL");
        OptionsValidation.Require(failures, options.Password, "QUERYAI_ADMIN_PASSWORD");

        if (!OptionsValidation.IsMissingOrPlaceholder(options.Email)
            && (!MailAddress.TryCreate(options.Email, out var parsed) || parsed.Address != options.Email))
        {
            failures.Add("A variável de ambiente QUERYAI_ADMIN_EMAIL deve conter um e-mail válido.");
        }

        if (!OptionsValidation.IsMissingOrPlaceholder(options.Password) && PasswordPolicy.Validate(options.Password).IsFailure)
        {
            failures.Add($"A variável de ambiente QUERYAI_ADMIN_PASSWORD deve ter entre {PasswordPolicy.MinLength} e {PasswordPolicy.MaxLength} caracteres.");
        }

        if (options.Name.Length is < User.NameMinLength or > User.NameMaxLength)
        {
            failures.Add($"A variável de ambiente QUERYAI_ADMIN_NAME deve ter entre {User.NameMinLength} e {User.NameMaxLength} caracteres.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
