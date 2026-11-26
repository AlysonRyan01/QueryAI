using FluentValidation;
using QueryAI.Domain.Users.Entities;

namespace QueryAI.Application.Common.Validations;

internal static class ValidationRules
{
    public static IRuleBuilderOptions<T, string> PasswordPolicy<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(Domain.Users.Policies.PasswordPolicy.MinLength)
            .WithMessage($"A senha deve ter no mínimo {Domain.Users.Policies.PasswordPolicy.MinLength} caracteres.")
            .MaximumLength(Domain.Users.Policies.PasswordPolicy.MaxLength)
            .WithMessage($"A senha deve ter no máximo {Domain.Users.Policies.PasswordPolicy.MaxLength} caracteres.");

    public static IRuleBuilderOptions<T, string> UserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(n => n is not null && n.Trim().Length is >= User.NameMinLength and <= User.NameMaxLength)
            .WithMessage($"O nome deve ter entre {User.NameMinLength} e {User.NameMaxLength} caracteres.");

    public static IRuleBuilderOptions<T, string> UserEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Informe o e-mail.")
            .MaximumLength(User.EmailMaxLength).WithMessage($"O e-mail deve ter no máximo {User.EmailMaxLength} caracteres.")
            .Must(BeValidEmail).WithMessage("Informe um e-mail válido.");

    private static bool BeValidEmail(string email) =>
        System.Net.Mail.MailAddress.TryCreate(email?.Trim(), out var parsed) && parsed.Address == email!.Trim();
}
