using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Application.Abstractions.Security.Enums;
using QueryAI.Application.Abstractions.Security.Interfaces;
using QueryAI.Application.Auth.Dtos;
using QueryAI.Application.Auth.Requests;
using QueryAI.Application.Auth.Validations;
using QueryAI.Application.Common.Extensions;
using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Common.Errors;
using QueryAI.Domain.Common.Interfaces;
using QueryAI.Domain.Common.Results;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Errors;

namespace QueryAI.Application.Auth.Services;

public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ISecurityAuditWriter audit,
    IClock clock)
{
    private static readonly LoginRequestValidator LoginValidator = new();
    private static readonly ChangePasswordRequestValidator ChangePasswordValidator = new();
    private static string? s_dummyHash;

    public async Task<Result<AuthenticatedSession>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await LoginValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var now = clock.UtcNow;
        var user = await users.GetByNormalizedEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);

        if (user is null || !user.IsActive)
        {
            passwordHasher.Verify(GetDummyHash(), request.Password);
            audit.Record(SecurityEventType.LoginFailed, actorUserId: null, targetUserId: user?.Id,
                new Dictionary<string, string> { ["reason"] = user is null ? "unknown_email" : "inactive" });
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return UserErrors.InvalidCredentials;
        }

        if (user.IsLockedOut(now))
        {
            audit.Record(SecurityEventType.LoginFailed, null, user.Id, new Dictionary<string, string> { ["reason"] = "locked_out" });
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return UserErrors.AccountLocked(user.LockoutRemaining(now));
        }

        var verification = passwordHasher.Verify(user.PasswordHash, request.Password);
        if (verification == PasswordVerification.Failed)
        {
            var lockedNow = user.RegisterFailedLogin(now);
            audit.Record(SecurityEventType.LoginFailed, null, user.Id, new Dictionary<string, string> { ["reason"] = "invalid_password" });
            if (lockedNow)
            {
                audit.Record(SecurityEventType.AccountLockedOut, null, user.Id);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return lockedNow ? UserErrors.AccountLocked(user.LockoutRemaining(now)) : UserErrors.InvalidCredentials;
        }

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            user.UpgradePasswordHash(passwordHasher.Hash(request.Password));
        }

        user.RegisterSuccessfulLogin(now);
        audit.Record(SecurityEventType.LoginSucceeded, user.Id, user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticatedSession(CurrentUserDto.From(user), user.SecurityStamp);
    }

    public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        audit.Record(SecurityEventType.Logout, userId, userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is { IsActive: true }
            ? CurrentUserDto.From(user)
            : Error.Unauthorized("unauthorized", "Sessão inválida.");
    }

    public async Task<Result<AuthenticatedSession>> ChangePasswordAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await ChangePasswordValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return UserErrors.InvalidCredentials;
        }

        if (passwordHasher.Verify(user.PasswordHash, request.CurrentPassword) == PasswordVerification.Failed)
        {
            return UserErrors.InvalidCurrentPassword;
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return UserErrors.SamePassword;
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword), clock.UtcNow);
        audit.Record(SecurityEventType.PasswordChanged, user.Id, user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticatedSession(CurrentUserDto.From(user), user.SecurityStamp);
    }

    private string GetDummyHash() => s_dummyHash ??= passwordHasher.Hash("queryai-timing-equalizer");
}
