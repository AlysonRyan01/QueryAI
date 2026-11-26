using QueryAI.Domain.Common.Interfaces;

namespace QueryAI.Infrastructure.Common.Services;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
