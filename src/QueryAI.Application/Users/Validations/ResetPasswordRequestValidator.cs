using FluentValidation;
using QueryAI.Application.Common.Validations;
using QueryAI.Application.Users.Requests;

namespace QueryAI.Application.Users.Validations;

internal sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(r => r.TemporaryPassword).PasswordPolicy();
    }
}
