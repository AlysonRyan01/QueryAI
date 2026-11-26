using QueryAI.Application.Abstractions.Persistence.Exceptions;
using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Application.Abstractions.Security.Interfaces;
using QueryAI.Application.Common.Extensions;
using QueryAI.Application.Users.Dtos;
using QueryAI.Application.Users.Requests;
using QueryAI.Application.Users.Validations;
using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Common.Interfaces;
using QueryAI.Domain.Common.Results;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;
using QueryAI.Domain.Users.Errors;

namespace QueryAI.Application.Users.Services;

public sealed class UserService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ISecurityAuditWriter audit,
    ICurrentUser currentUser,
    IClock clock)
{
    private static readonly ListUsersRequestValidator ListValidator = new();
    private static readonly CreateUserRequestValidator CreateValidator = new();
    private static readonly UpdateUserRequestValidator UpdateValidator = new();
    private static readonly ResetPasswordRequestValidator ResetPasswordValidator = new();

    public async Task<Result<UserListDto>> ListAsync(ListUsersRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await ListValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var page = await users.ListAsync(request.Search, request.Page, request.PageSize, cancellationToken);
        var now = clock.UtcNow;
        return new UserListDto(page.Items.Select(u => UserDto.From(u, now)).ToList(), page.Total, page.Page, page.PageSize);
    }

    public async Task<Result<UserDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        return user is null ? UserErrors.NotFound : UserDto.From(user, clock.UtcNow);
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await CreateValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (await users.ExistsByNormalizedEmailAsync(User.NormalizeEmail(request.Email), cancellationToken))
        {
            return UserErrors.EmailAlreadyExists;
        }

        var now = clock.UtcNow;
        var user = User.Create(
            request.Name, request.Email, passwordHasher.Hash(request.TemporaryPassword), request.Role,
            mustChangePassword: true, now);
        users.Add(user);
        audit.Record(SecurityEventType.UserCreated, currentUser.UserId, user.Id,
            new Dictionary<string, string> { ["role"] = user.Role.ToString() });

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            return UserErrors.EmailAlreadyExists;
        }

        return UserDto.From(user, now);
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await UpdateValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync<Result<UserDto>>(async ct =>
            {
                var user = await users.GetByIdAsync(id, ct);
                if (user is null)
                {
                    return UserErrors.NotFound;
                }

                var demotingActiveAdmin = user is { Role: Role.Admin, IsActive: true } && request.Role != Role.Admin;
                if (demotingActiveAdmin && await users.CountOtherActiveAdminsForUpdateAsync(user.Id, ct) == 0)
                {
                    return UserErrors.LastAdmin;
                }

                var now = clock.UtcNow;
                var previousRole = user.Role;
                user.Rename(request.Name, now);
                user.ChangeRole(request.Role, now);

                audit.Record(SecurityEventType.UserUpdated, currentUser.UserId, user.Id);
                if (previousRole != user.Role)
                {
                    audit.Record(SecurityEventType.UserRoleChanged, currentUser.UserId, user.Id,
                        new Dictionary<string, string> { ["from"] = previousRole.ToString(), ["to"] = user.Role.ToString() });
                }

                return UserDto.From(user, now);
            }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return UserErrors.ConcurrencyConflict;
        }
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync<Result>(async ct =>
            {
                var user = await users.GetByIdAsync(id, ct);
                if (user is null)
                {
                    return UserErrors.NotFound;
                }

                if (!user.IsActive)
                {
                    return Result.Success();
                }

                if (user.Role == Role.Admin && await users.CountOtherActiveAdminsForUpdateAsync(user.Id, ct) == 0)
                {
                    return UserErrors.LastAdmin;
                }

                user.Deactivate(clock.UtcNow);
                audit.Record(SecurityEventType.UserDeactivated, currentUser.UserId, user.Id);
                return Result.Success();
            }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return UserErrors.ConcurrencyConflict;
        }
    }

    public async Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        if (!user.IsActive)
        {
            user.Activate(clock.UtcNow);
            audit.Record(SecurityEventType.UserActivated, currentUser.UserId, user.Id);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await ResetPasswordValidator.ValidateToErrorAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        user.ResetPassword(passwordHasher.Hash(request.TemporaryPassword), clock.UtcNow);
        audit.Record(SecurityEventType.PasswordReset, currentUser.UserId, user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
