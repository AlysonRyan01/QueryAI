using FluentValidation;
using QueryAI.Application.Users.Requests;

namespace QueryAI.Application.Users.Validations;

internal sealed class ListUsersRequestValidator : AbstractValidator<ListUsersRequest>
{
    public ListUsersRequestValidator()
    {
        RuleFor(r => r.Search).MaximumLength(100);
        RuleFor(r => r.Page).GreaterThanOrEqualTo(1);
        RuleFor(r => r.PageSize).InclusiveBetween(1, 100);
    }
}
