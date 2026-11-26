namespace QueryAI.Domain.Auditing.Enums;

public enum SecurityEventType
{
    LoginSucceeded,
    LoginFailed,
    AccountLockedOut,
    Logout,
    PasswordChanged,
    PasswordReset,
    UserCreated,
    UserUpdated,
    UserRoleChanged,
    UserDeactivated,
    UserActivated,
    InitialAdminCreated,
}
