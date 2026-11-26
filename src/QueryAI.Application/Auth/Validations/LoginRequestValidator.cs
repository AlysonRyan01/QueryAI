using FluentValidation;
using QueryAI.Application.Auth.Requests;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Policies;

namespace QueryAI.Application.Auth.Validations;

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().WithMessage("Informe o e-mail.").MaximumLength(User.EmailMaxLength);
        RuleFor(r => r.Password).NotEmpty().WithMessage("Informe a senha.").MaximumLength(PasswordPolicy.MaxLength);
    }
}
