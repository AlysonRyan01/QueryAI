using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Auth.Dtos;

public sealed record CurrentUserDto(Guid Id, string Name, string Email, Role Role, bool MustChangePassword)
{
    public static CurrentUserDto From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new CurrentUserDto(user.Id, user.Name, user.Email, user.Role, user.MustChangePassword);
    }
}
