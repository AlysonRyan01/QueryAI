using QueryAI.Domain.Common.Results;
using QueryAI.Domain.Users.Errors;

namespace QueryAI.Domain.Users.Policies;

public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    public static Result Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            return UserErrors.InvalidPassword($"A senha deve ter no mínimo {MinLength} caracteres.");
        }

        if (password.Length > MaxLength)
        {
            return UserErrors.InvalidPassword($"A senha deve ter no máximo {MaxLength} caracteres.");
        }

        return Result.Success();
    }
}
