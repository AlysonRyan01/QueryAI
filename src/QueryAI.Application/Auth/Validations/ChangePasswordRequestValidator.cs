using FluentValidation;
using QueryAI.Application.Auth.Requests;
using QueryAI.Application.Common.Validations;
using QueryAI.Domain.Users.Policies;

namespace QueryAI.Application.Auth.Validations;

internal sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(r => r.CurrentPassword).NotEmpty().WithMessage("Informe a senha atual.").MaximumLength(PasswordPolicy.MaxLength);
        RuleFor(r => r.NewPassword).PasswordPolicy();
    }
}
