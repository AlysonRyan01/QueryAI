using FluentValidation;
using QueryAI.Application.Common.Validations;
using QueryAI.Application.Users.Requests;

namespace QueryAI.Application.Users.Validations;

internal sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(r => r.Name).UserName();
        RuleFor(r => r.Email).UserEmail();
        RuleFor(r => r.Role).IsInEnum().WithMessage("Papel inválido.");
        RuleFor(r => r.TemporaryPassword).PasswordPolicy();
    }
}
