using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Common.Entities;

namespace QueryAI.Domain.Auditing.Entities;

public sealed class SecurityAuditEvent : Entity
{
    public const int IpAddressMaxLength = 45;
    public const int UserAgentMaxLength = 512;

    private SecurityAuditEvent()
    {
    }

    public SecurityEventType EventType { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? TargetUserId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public IReadOnlyDictionary<string, string>? Details { get; private set; }

    public static SecurityAuditEvent Create(
        SecurityEventType eventType,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        Guid? targetUserId,
        string? ipAddress,
        string? userAgent,
        IReadOnlyDictionary<string, string>? details = null) =>
        new()
        {
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            IpAddress = Truncate(ipAddress, IpAddressMaxLength),
            UserAgent = Truncate(userAgent, UserAgentMaxLength),
            Details = details is { Count: > 0 } ? new Dictionary<string, string>(details) : null,
        };

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
