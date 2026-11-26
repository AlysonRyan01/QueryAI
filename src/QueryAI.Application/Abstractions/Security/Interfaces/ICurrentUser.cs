namespace QueryAI.Application.Abstractions.Security.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}
