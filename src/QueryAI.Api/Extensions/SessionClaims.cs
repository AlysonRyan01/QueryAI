using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using QueryAI.Application.Auth.Dtos;

namespace QueryAI.Api.Extensions;

internal static class SessionClaims
{
    public const string UserId = "sub";
    public const string Name = "name";
    public const string Role = "role";
    public const string SecurityStamp = "stamp";
    public const string MustChangePassword = "mcp";

    public static ClaimsPrincipal CreatePrincipal(CurrentUserDto user, Guid securityStamp)
    {
        ArgumentNullException.ThrowIfNull(user);
        var identity = new ClaimsIdentity(
            [
                new Claim(UserId, user.Id.ToString()),
                new Claim(Name, user.Name),
                new Claim(Role, user.Role.ToString()),
                new Claim(SecurityStamp, securityStamp.ToString()),
                new Claim(MustChangePassword, user.MustChangePassword.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme,
            Name,
            Role);
        return new ClaimsPrincipal(identity);
    }

    public static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst(UserId)?.Value, out var id) ? id : null;

    public static bool MustChangePasswordFlag(this ClaimsPrincipal principal) =>
        string.Equals(principal.FindFirst(MustChangePassword)?.Value, "true", StringComparison.Ordinal);
}
