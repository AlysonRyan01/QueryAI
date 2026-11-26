using QueryAI.Domain.Common.Interfaces;

namespace QueryAI.Application.Tests.Fakes;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
}
