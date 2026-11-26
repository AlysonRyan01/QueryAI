using Microsoft.Extensions.Logging;
using QueryAI.Application.Abstractions.Security.Interfaces;
using QueryAI.Domain.Auditing.Entities;
using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Common.Interfaces;
using QueryAI.Infrastructure.Persistence.Contexts;

namespace QueryAI.Infrastructure.Security.Services;

internal sealed partial class SecurityAuditWriter(
    AppDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    ILogger<SecurityAuditWriter> logger) : ISecurityAuditWriter
{
    public void Record(
        SecurityEventType eventType,
        Guid? actorUserId,
        Guid? targetUserId,
        IReadOnlyDictionary<string, string>? details = null)
    {
        var auditEvent = SecurityAuditEvent.Create(
            eventType, clock.UtcNow, actorUserId, targetUserId, currentUser.IpAddress, currentUser.UserAgent, details);
        db.SecurityAuditEvents.Add(auditEvent);

        LogSecurityEvent(logger, eventType, actorUserId, targetUserId, auditEvent.IpAddress);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Security event {EventType} actor={ActorUserId} target={TargetUserId} ip={IpAddress}")]
    private static partial void LogSecurityEvent(
        ILogger logger, SecurityEventType eventType, Guid? actorUserId, Guid? targetUserId, string? ipAddress);
}
