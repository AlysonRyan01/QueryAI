using Microsoft.EntityFrameworkCore;
using Npgsql;
using QueryAI.Application.Abstractions.Persistence.Exceptions;
using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Domain.Common.Results;
using QueryAI.Infrastructure.Persistence.Contexts;

namespace QueryAI.Infrastructure.Persistence.Transactions;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The record was modified concurrently.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateKeyException("A unique constraint was violated.", ex);
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await operation(cancellationToken);
        if (result.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        await SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
