using QueryAI.Application.Abstractions.Persistence.Models;
using QueryAI.Domain.Users.Entities;

namespace QueryAI.Application.Abstractions.Persistence.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    void Add(User user);

    Task<PagedResult<User>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountOtherActiveAdminsForUpdateAsync(Guid excludeUserId, CancellationToken cancellationToken);
}
