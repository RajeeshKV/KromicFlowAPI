using KromicFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Abstractions;

public interface IKromicFlowDbContext
{
    DbSet<User> Users { get; }
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<Session> Sessions { get; }
    DbSet<Plan> Plans { get; }
    DbSet<UserRestriction> UserRestrictions { get; }
    DbSet<InstagramAccount> InstagramAccounts { get; }
    DbSet<Automation> Automations { get; }
    DbSet<WebhookEvent> WebhookEvents { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<UserActivity> UserActivities { get; }
    DbSet<RuntimeSetting> RuntimeSettings { get; }
    DbSet<TermsAcceptance> TermsAcceptances { get; }
    DbSet<NotificationMessage> NotificationMessages { get; }
    DbSet<OutboxEvent> OutboxEvents { get; }
    DbSet<DeadLetterEvent> DeadLetterEvents { get; }
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
