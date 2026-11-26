using QueryAI.Application.Abstractions.Security.Interfaces;

namespace QueryAI.Application.Tests.Fakes;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; } = Guid.NewGuid();

    public string? IpAddress => "127.0.0.1";

    public string? UserAgent => "tests";
}
