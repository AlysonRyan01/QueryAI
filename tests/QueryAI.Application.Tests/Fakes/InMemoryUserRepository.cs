using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Application.Abstractions.Persistence.Models;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Tests.Fakes;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail));

    public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Users.Any(u => u.NormalizedEmail == normalizedEmail));

    public void Add(User user) => Users.Add(user);

    public Task<PagedResult<User>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult(new PagedResult<User>(Users.Skip((page - 1) * pageSize).Take(pageSize).ToList(), Users.Count, page, pageSize));

    public Task<int> CountOtherActiveAdminsForUpdateAsync(Guid excludeUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Users.Count(u => u.Role == Role.Admin && u.IsActive && u.Id != excludeUserId));
}
