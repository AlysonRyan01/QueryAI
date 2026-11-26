using FluentValidation;
using QueryAI.Application.Common.Validations;
using QueryAI.Application.Users.Requests;

namespace QueryAI.Application.Users.Validations;

internal sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(r => r.Name).UserName();
        RuleFor(r => r.Role).IsInEnum().WithMessage("Papel inválido.");
    }
}
