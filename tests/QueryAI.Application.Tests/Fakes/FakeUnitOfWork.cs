using QueryAI.Application.Abstractions.Persistence.Interfaces;
using QueryAI.Domain.Common.Results;

namespace QueryAI.Application.Tests.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Exception? ThrowOnSave { get; set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (ThrowOnSave is not null)
        {
            throw ThrowOnSave;
        }

        SaveCount++;
        return Task.CompletedTask;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        where TResult : Result
    {
        var result = await operation(cancellationToken);
        if (result.IsSuccess)
        {
            await SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
