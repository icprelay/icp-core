using Microsoft.EntityFrameworkCore;

namespace Icp.Persistence.Data.Runtime;

public class RuntimeDbContext : DbContext
{
    public RuntimeDbContext(DbContextOptions<RuntimeDbContext> options) : base(options)
    {
    }

    public DbSet<IntegrationAccount> IntegrationAccounts => Set<IntegrationAccount>();
    public DbSet<IntegrationInstance> IntegrationInstances => Set<IntegrationInstance>();
    public DbSet<Run> Runs => Set<Run>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<IntegrationTarget> IntegrationTargets => Set<IntegrationTarget>();
    public DbSet<ScheduleTimeZone> ScheduleTimeZones => Set<ScheduleTimeZone>();
    public DbSet<EventTrace> EventTraces => Set<EventTrace>();
    public DbSet<EventStep> EventSteps => Set<EventStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.HasKey(x => x.Name);
            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.AllowedTriggerTypes)
                .HasMaxLength(50)
                .HasDefaultValue("Event");

            entity.Property(x => x.ParametersTemplateJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("{}");
        });

        modelBuilder.Entity<IntegrationTarget>(entity =>
        {
            entity.HasKey(x => x.Name);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.ParametersTemplateJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.SecretsTemplateJson).HasColumnType("nvarchar(max)");

            entity.Property(x => x.AllowedTriggerTypes)
                .HasMaxLength(50)
                .HasDefaultValue("Event");
        });

        modelBuilder.Entity<ScheduleTimeZone>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(100);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Enabled).HasDefaultValue(true);
            entity.HasIndex(x => x.Enabled);
            entity.HasIndex(x => x.SortOrder);
        });

        modelBuilder.Entity<IntegrationAccount>(entity =>
        {
            entity.HasKey(x => x.AccountId);

            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.ExternalCustomerId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.InboundKeyHash).HasMaxLength(200);

            entity.HasIndex(x => x.ExternalCustomerId).IsUnique();

            entity.HasMany(x => x.IntegrationInstances)
                .WithOne(x => x.Account)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IntegrationInstance>(entity =>
        {
            entity.HasKey(x => x.InstanceId);

            entity.Property(x => x.CustomerId).HasMaxLength(200);
            entity.Property(x => x.IntegrationTarget).HasMaxLength(200);
            entity.Property(x => x.DisplayName).HasMaxLength(200);

            entity.Property(x => x.SubscribedEventType).HasMaxLength(200);

            entity.Property(x => x.IntegrationTargetParametersJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.EventParametersJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.SecretRefsJson).HasColumnType("nvarchar(max)");

            entity.Property(x => x.TriggerType)
                .HasMaxLength(20)
                .HasDefaultValue("Event");

            entity.Property(x => x.ScheduleCron)
                .HasMaxLength(100);

            entity.Property(x => x.ScheduleTimeZone)
                .HasMaxLength(100);

            entity.Property(x => x.ScheduleVersion)
                .HasDefaultValue(1);

            entity.Property(x => x.RowVersion)
                .IsRowVersion();

            entity.HasIndex(x => x.AccountId);

            entity.HasOne(x => x.Target)
                .WithMany(x => x.IntegrationInstances)
                .HasForeignKey(x => x.IntegrationTarget)
                .HasPrincipalKey(x => x.Name)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EventType)
                .WithMany(x => x.IntegrationInstances)
                .HasForeignKey(x => x.SubscribedEventType)
                .HasPrincipalKey(x => x.Name)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Runs)
                .WithOne(x => x.Instance)
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(x => x.DeletedAt == null);

            entity.HasIndex(x => new { x.CustomerId, x.IntegrationTarget });
            entity.HasIndex(x => x.IntegrationTarget);
            entity.HasIndex(x => x.SubscribedEventType);
            entity.HasIndex(x => x.DeletedAt);
        });

        modelBuilder.Entity<Run>(entity =>
        {
            entity.HasKey(x => x.RunId);

            entity.Property(x => x.CorrelationId).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.Error).HasColumnType("nvarchar(max)");
            entity.Property(x => x.OutputFullBlobPath).HasColumnType("nvarchar(max)");

            entity.HasIndex(x => new { x.InstanceId, x.CreatedAt });
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<EventTrace>(entity =>
        {
            entity.HasKey(x => x.EventId);

            entity.Property(x => x.CorrelationId).HasMaxLength(200);
            entity.Property(x => x.AccountKey).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CurrentStage).HasMaxLength(50).IsRequired();
            entity.Property(x => x.BlobRef).HasMaxLength(500);

            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.AccountKey);
            entity.HasIndex(x => x.ReceivedAtUtc);
        });

        modelBuilder.Entity<EventStep>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.StepName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.LogicAppRunId).HasMaxLength(500);
            entity.Property(x => x.Message).HasColumnType("nvarchar(max)");
            entity.Property(x => x.TargetType).HasMaxLength(200);

            entity.HasIndex(x => x.EventId);
            entity.HasIndex(x => new { x.EventId, x.StartedAtUtc });
        });
    }
}
