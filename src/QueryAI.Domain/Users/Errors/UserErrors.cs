using QueryAI.Domain.Common.Enums;
using QueryAI.Domain.Common.Errors;

namespace QueryAI.Domain.Users.Errors;

public static class UserErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("invalid_credentials", "E-mail ou senha inválidos.");

    public static readonly Error InvalidCurrentPassword =
        Error.Unauthorized("invalid_current_password", "A senha atual está incorreta.");

    public static readonly Error SamePassword =
        Error.Validation("same_password", "A nova senha deve ser diferente da senha atual.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict("email_already_exists", "Já existe um usuário com este e-mail.");

    public static readonly Error LastAdmin =
        Error.Conflict("last_admin", "A instância precisa de pelo menos um administrador ativo.");

    public static readonly Error NotFound =
        Error.NotFound("not_found", "Usuário não encontrado.");

    public static readonly Error PasswordChangeRequired =
        Error.Forbidden("password_change_required", "É necessário trocar a senha antes de continuar.");

    public static readonly Error ConcurrencyConflict =
        Error.Conflict("concurrency_conflict", "O registro foi alterado por outra pessoa. Recarregue e tente novamente.");

    public static Error AccountLocked(TimeSpan remaining)
    {
        var seconds = (int)Math.Ceiling(Math.Max(remaining.TotalSeconds, 1));
        var minutes = (int)Math.Ceiling(seconds / 60.0);
        return new Error(
            "account_locked",
            $"Conta bloqueada temporariamente. Tente novamente em {minutes} minuto(s).",
            ErrorKind.Locked)
        {
            Metadata = new Dictionary<string, object> { ["retryAfterSeconds"] = seconds },
        };
    }

    public static Error InvalidPassword(string message) =>
        Error.Validation("validation_failed", message, new Dictionary<string, string[]> { ["password"] = [message] });
}
