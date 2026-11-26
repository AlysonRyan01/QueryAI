using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    Role Role,
    bool IsActive,
    bool IsLockedOut,
    bool MustChangePassword,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc)
{
    public static UserDto From(User user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserDto(
            user.Id, user.Name, user.Email, user.Role, user.IsActive, user.IsLockedOut(now),
            user.MustChangePassword, user.CreatedAtUtc, user.LastLoginAtUtc);
    }
}
