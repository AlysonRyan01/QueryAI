using QueryAI.Application.Abstractions.Security.Interfaces;
using QueryAI.Domain.Auditing.Enums;

namespace QueryAI.Application.Tests.Fakes;

internal sealed class RecordingAuditWriter : ISecurityAuditWriter
{
    public List<(SecurityEventType Type, Guid? Actor, Guid? Target, IReadOnlyDictionary<string, string>? Details)> Events { get; } = [];

    public void Record(SecurityEventType eventType, Guid? actorUserId, Guid? targetUserId, IReadOnlyDictionary<string, string>? details = null) =>
        Events.Add((eventType, actorUserId, targetUserId, details));
}
