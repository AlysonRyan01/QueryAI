using QueryAI.Domain.Common.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Domain.Users.Entities;

public sealed class User : Entity
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 120;
    public const int EmailMaxLength = 254;
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private User()
    {
        Name = string.Empty;
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        PasswordHash = string.Empty;
    }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string NormalizedEmail { get; private set; }

    public string PasswordHash { get; private set; }

    public Role Role { get; private set; }

    public bool IsActive { get; private set; }

    public bool MustChangePassword { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public Guid SecurityStamp { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public static User Create(
        string name,
        string email,
        string passwordHash,
        Role role,
        bool mustChangePassword,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var user = new User
        {
            Name = NormalizeName(name),
            Email = NormalizeEmailForStorage(email),
            NormalizedEmail = NormalizeEmail(email),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            MustChangePassword = mustChangePassword,
            SecurityStamp = Guid.NewGuid(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        return user;
    }

    public static string NormalizeEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToUpperInvariant();
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndUtc is { } end && end > now;

    public TimeSpan LockoutRemaining(DateTimeOffset now) =>
        LockoutEndUtc is { } end && end > now ? end - now : TimeSpan.Zero;

    public bool RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount < MaxFailedLoginAttempts)
        {
            return false;
        }

        LockoutEndUtc = now + LockoutDuration;
        FailedLoginCount = 0;
        UpdatedAtUtc = now;
        return true;
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = now;
    }

    public void UpgradePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void ChangePassword(string newPasswordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        RotateSecurityStamp(now);
    }

    public void ResetPassword(string temporaryPasswordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPasswordHash);
        PasswordHash = temporaryPasswordHash;
        MustChangePassword = true;
        FailedLoginCount = 0;
        LockoutEndUtc = null;
        RotateSecurityStamp(now);
    }

    public void Rename(string name, DateTimeOffset now)
    {
        Name = NormalizeName(name);
        UpdatedAtUtc = now;
    }

    public void ChangeRole(Role role, DateTimeOffset now)
    {
        if (Role == role)
        {
            return;
        }

        Role = role;
        RotateSecurityStamp(now);
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RotateSecurityStamp(now);
    }

    public void Activate(DateTimeOffset now)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAtUtc = now;
    }

    private void RotateSecurityStamp(DateTimeOffset now)
    {
        SecurityStamp = Guid.NewGuid();
        UpdatedAtUtc = now;
    }

    private static string NormalizeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var trimmed = name.Trim();
        if (trimmed.Length is < NameMinLength or > NameMaxLength)
        {
            throw new ArgumentException(
                $"Name must have between {NameMinLength} and {NameMaxLength} characters.", nameof(name));
        }

        return trimmed;
    }

    private static string NormalizeEmailForStorage(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var trimmed = email.Trim();
        if (trimmed.Length > EmailMaxLength)
        {
            throw new ArgumentException($"E-mail must have at most {EmailMaxLength} characters.", nameof(email));
        }

        return trimmed;
    }
}
