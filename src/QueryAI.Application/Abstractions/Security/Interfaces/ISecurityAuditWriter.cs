using QueryAI.Domain.Auditing.Enums;

namespace QueryAI.Application.Abstractions.Security.Interfaces;

public interface ISecurityAuditWriter
{
    void Record(
        SecurityEventType eventType,
        Guid? actorUserId,
        Guid? targetUserId,
        IReadOnlyDictionary<string, string>? details = null);
}
