namespace QueryAI.Domain.Common.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
