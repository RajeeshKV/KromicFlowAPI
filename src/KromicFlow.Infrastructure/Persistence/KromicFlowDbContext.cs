using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Infrastructure.Persistence;

public sealed class KromicFlowDbContext(DbContextOptions<KromicFlowDbContext> options) : DbContext(options), IKromicFlowDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<UserRestriction> UserRestrictions => Set<UserRestriction>();
    public DbSet<InstagramAccount> InstagramAccounts => Set<InstagramAccount>();
    public DbSet<InstagramMedia> InstagramMedia => Set<InstagramMedia>();
    public DbSet<Automation> Automations => Set<Automation>();
    public DbSet<AutomationMedia> AutomationMedia => Set<AutomationMedia>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserActivity> UserActivities => Set<UserActivity>();
    public DbSet<RuntimeSetting> RuntimeSettings => Set<RuntimeSetting>();
    public DbSet<TermsAcceptance> TermsAcceptances => Set<TermsAcceptance>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<DeadLetterEvent> DeadLetterEvents => Set<DeadLetterEvent>();

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new DbTransaction(transaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureAuth(modelBuilder);
        ConfigurePlans(modelBuilder);
        ConfigureInstagram(modelBuilder);
        ConfigureOperationalTables(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedUtc = now;
            if (entry.State == EntityState.Modified) entry.Entity.UpdatedUtc = now;
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRestriction>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.HasOne(x => x.User).WithOne(x => x.Restriction).HasForeignKey<UserRestriction>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserActivity>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.CreatedUtc });
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuth(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(100);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.PasswordHash).HasMaxLength(500);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasIndex(x => x.SessionGuid).IsUnique();
            entity.HasIndex(x => x.RefreshTokenHash).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.AdminUserId);
            entity.Property(x => x.RefreshTokenHash).HasMaxLength(256);
            entity.Property(x => x.DeviceName).HasMaxLength(200);
            entity.Property(x => x.Browser).HasMaxLength(120);
            entity.Property(x => x.OS).HasMaxLength(120);
            entity.Property(x => x.IPAddress).HasMaxLength(80);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasOne(x => x.User).WithMany(x => x.Sessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AdminUser).WithMany().HasForeignKey(x => x.AdminUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.ConfigurationJson).HasColumnType("jsonb");
            entity.HasData(new Plan
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Code = "free",
                Name = "Free",
                IsActive = true,
                IsDefault = true,
                MaxInstagramAccounts = 1,
                MaxAutomations = 3,
                MonthlyAutomationRuns = 100,
                MonthlyEmails = 25,
                MonthlyPushNotifications = 25,
                ConfigurationJson = "{}",
                CreatedUtc = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        modelBuilder.Entity<RuntimeSetting>(entity =>
        {
            entity.HasIndex(x => x.Key).IsUnique();
            entity.Property(x => x.Key).HasMaxLength(160);
            entity.Property(x => x.Value).HasColumnType("text");
            entity.Property(x => x.Description).HasMaxLength(500);
        });
    }

    private static void ConfigureInstagram(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstagramAccount>(entity =>
        {
            // Unique constraints to prevent duplicate accounts
            entity.HasIndex(x => x.InstagramUserId).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.InstagramUserId });
            
            // Property configurations
            entity.Property(x => x.InstagramUserId).HasMaxLength(100);
            entity.Property(x => x.FacebookPageId).HasMaxLength(100);
            entity.Property(x => x.Username).HasMaxLength(160);
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.ProfilePicture).HasColumnType("text");
            entity.Property(x => x.AccessTokenEncrypted).HasColumnType("text");
            entity.Property(x => x.TokenStatus).HasMaxLength(20).HasDefaultValue("active");
            entity.Property(x => x.Version).IsRowVersion();
            
            entity.HasOne(x => x.User).WithMany(x => x.InstagramAccounts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Automation>(entity =>
        {
            entity.HasIndex(x => x.InstagramAccountId);
            entity.HasIndex(x => x.Scope);
            entity.HasIndex(x => x.Enabled);
            entity.Property(x => x.TriggerType).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Scope).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.KeywordsJson).HasColumnType("jsonb");
            entity.Property(x => x.PublicReply).HasMaxLength(2000);
            entity.Property(x => x.PrivateReply).HasMaxLength(2000);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasOne(x => x.InstagramAccount).WithMany(x => x.Automations).HasForeignKey(x => x.InstagramAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstagramMedia>(entity =>
        {
            entity.HasIndex(x => x.InstagramMediaId).IsUnique();
            entity.HasIndex(x => x.InstagramAccountId);
            entity.HasIndex(x => x.PostedAtUtc);
            entity.HasIndex(x => x.MediaType);
            entity.Property(x => x.MediaType).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Caption).HasMaxLength(2200);
            entity.Property(x => x.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(x => x.MediaUrl).HasMaxLength(1000);
            entity.Property(x => x.Permalink).HasMaxLength(1000);
            entity.HasOne(x => x.InstagramAccount).WithMany().HasForeignKey(x => x.InstagramAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AutomationMedia>(entity =>
        {
            entity.HasIndex(x => x.AutomationId);
            entity.HasIndex(x => x.InstagramMediaId);
            entity.HasIndex(x => new { x.AutomationId, x.InstagramMediaId }).IsUnique();
            entity.HasOne(x => x.Automation).WithMany(x => x.AutomationMedia).HasForeignKey(x => x.AutomationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.InstagramMedia).WithMany(x => x.AutomationMedia).HasForeignKey(x => x.InstagramMediaId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureOperationalTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Payload).HasColumnType("jsonb");
            entity.Property(x => x.FailureReason).HasMaxLength(2000);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(x => x.CreatedUtc);
            entity.Property(x => x.Action).HasMaxLength(160);
            entity.Property(x => x.EntityName).HasMaxLength(160);
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.DetailsJson).HasColumnType("jsonb");
            entity.Property(x => x.IPAddress).HasMaxLength(80);
        });

        modelBuilder.Entity<TermsAcceptance>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.TermsVersion }).IsUnique();
            entity.Property(x => x.TermsVersion).HasMaxLength(80);
            entity.Property(x => x.IPAddress).HasMaxLength(80);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationMessage>(entity =>
        {
            entity.HasIndex(x => x.CreatedUtc);
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Audience).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(80);
            entity.Property(x => x.Subject).HasMaxLength(300);
            entity.Property(x => x.Body).HasColumnType("text");
            entity.Property(x => x.ProviderMessageId).HasMaxLength(200);
            entity.Property(x => x.FailureReason).HasMaxLength(2000);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasIndex(x => x.ProcessedUtc);
            entity.HasIndex(x => x.CreatedUtc);
            entity.Property(x => x.EventType).HasMaxLength(200);
            entity.Property(x => x.Payload).HasColumnType("jsonb");
            entity.Property(x => x.Error).HasColumnType("text");
        });

        modelBuilder.Entity<DeadLetterEvent>(entity =>
        {
            entity.HasIndex(x => x.FailedUtc);
            entity.Property(x => x.EventType).HasMaxLength(200);
            entity.Property(x => x.Payload).HasColumnType("jsonb");
            entity.Property(x => x.Error).HasColumnType("text");
        });
    }
}

