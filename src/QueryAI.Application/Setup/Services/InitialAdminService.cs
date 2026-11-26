using QueryAI.Application.Abstractions.Persistence.Exceptions;
using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Application.Abstractions.Security.Interfaces;
using QueryAI.Application.Setup.Enums;
using QueryAI.Application.Setup.Settings;
using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Common.Interfaces;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;
using QueryAI.Domain.Users.Policies;

namespace QueryAI.Application.Setup.Services;

public sealed class InitialAdminService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ISecurityAuditWriter audit,
    IClock clock)
{
    public async Task<InitialAdminOutcome> EnsureAsync(InitialAdminSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (await users.ExistsByNormalizedEmailAsync(User.NormalizeEmail(settings.Email), cancellationToken))
        {
            return InitialAdminOutcome.AlreadyExists;
        }

        var policy = PasswordPolicy.Validate(settings.Password);
        if (policy.IsFailure)
        {
            throw new InvalidOperationException($"QUERYAI_ADMIN_PASSWORD inválida: {policy.Error.Message}");
        }

        var user = User.Create(
            settings.Name, settings.Email, passwordHasher.Hash(settings.Password), Role.Admin, mustChangePassword: true, clock.UtcNow);
        users.Add(user);
        audit.Record(SecurityEventType.InitialAdminCreated, actorUserId: null, targetUserId: user.Id);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return InitialAdminOutcome.Created;
        }
        catch (DuplicateKeyException)
        {
            return InitialAdminOutcome.AlreadyExists;
        }
    }
}
