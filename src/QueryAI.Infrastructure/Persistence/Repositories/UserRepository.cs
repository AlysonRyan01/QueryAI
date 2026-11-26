using Microsoft.EntityFrameworkCore;
using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Application.Abstractions.Persistence.Models;
using QueryAI.Domain.Users.Entities;
using QueryAI.Infrastructure.Persistence.Contexts;

namespace QueryAI.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public async Task<PagedResult<User>> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = "%" + EscapeLike(search.Trim()) + "%";
            query = query.Where(u => EF.Functions.ILike(u.Name, pattern, "\\") || EF.Functions.ILike(u.Email, pattern, "\\"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>(items, total, page, pageSize);
    }

    public async Task<int> CountOtherActiveAdminsForUpdateAsync(Guid excludeUserId, CancellationToken cancellationToken)
    {
        var adminIds = await db.Database
            .SqlQuery<Guid>($"""SELECT id AS "Value" FROM users WHERE role = 'Admin' AND is_active FOR UPDATE""")
            .ToListAsync(cancellationToken);

        return adminIds.Count(id => id != excludeUserId);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
