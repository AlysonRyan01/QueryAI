using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryAI.Domain.Auditing.Entities;
using QueryAI.Domain.Users.Entities;

namespace QueryAI.Infrastructure.Persistence.Configurations;

internal sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
    {
        builder.ToTable("security_audit_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.IpAddress).HasMaxLength(SecurityAuditEvent.IpAddressMaxLength);
        builder.Property(e => e.UserAgent).HasMaxLength(SecurityAuditEvent.UserAgentMaxLength);
        builder.HasIndex(e => e.OccurredAtUtc);

        builder.Property(e => e.Details)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null),
                new ValueComparer<IReadOnlyDictionary<string, string>?>(
                    (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.Count == b.Count && !a.Except(b).Any()),
                    v => v == null ? 0 : v.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key, kv.Value)),
                    v => v == null ? null : new Dictionary<string, string>(v)));

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.TargetUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
