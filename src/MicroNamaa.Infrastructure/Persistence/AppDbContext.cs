using System.Linq.Expressions;

using System.Text.Json;

using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.ChangeTracking;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.DTOs.Notifications;

using MicroNamaa.Application.Exceptions;

using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence;

/// <summary>
/// The database. Rows of <see cref="IMerchantOwned" /> entities are filtered to the signed-in user's merchant, so one merchant
/// never sees another's data; platform admins, anonymous calls and background jobs have no merchant and see every row.
/// New rows get the user's merchant, and a merchant user cannot save a row of another merchant.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, IUserAppService userAppService, INotificationPublisher notificationPublisher) : IdentityDbContext<User, Role, int>(options)
{
    /// <summary>Read by the query filters on every query; empty means no merchant filter.</summary>
    public int? CurrentMerchantId => userAppService.MerchantId;

    public DbSet<Merchant> Merchants => Set<Merchant>();

    public DbSet<MerchantApplication> MerchantApplications => Set<MerchantApplication>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<Register> Registers => Set<Register>();

    public DbSet<DocumentCounter> DocumentCounters => Set<DocumentCounter>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerBalanceEntry> CustomerBalanceEntries => Set<CustomerBalanceEntry>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<SupplierBalanceEntry> SupplierBalanceEntries => Set<SupplierBalanceEntry>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();

    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();

    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();

    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();

    public DbSet<StaffPermission> StaffPermissions => Set<StaffPermission>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndSoftDelete();

        var pending = CollectAuditTrail();

        using var transaction = pending.Count > 0 && Database.CurrentTransaction is null ? Database.BeginTransaction() : null;

        var affected = base.SaveChanges(acceptAllChangesOnSuccess);

        if (pending.Count > 0)
        {
            AuditLogs.AddRange(pending.Select(CreateAuditLog));

            ApplyAuditAndSoftDelete();

            base.SaveChanges(acceptAllChangesOnSuccess);
        }

        transaction?.Commit();

        return affected;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();

        var pending = CollectAuditTrail();

        var newNotifications = ChangeTracker.Entries<Notification>().Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToList();

        await using var transaction = pending.Count > 0 && Database.CurrentTransaction is null ? await Database.BeginTransactionAsync(cancellationToken) : null;

        var affected = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (pending.Count > 0)
        {
            AuditLogs.AddRange(pending.Select(CreateAuditLog));

            ApplyAuditAndSoftDelete();

            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        if (newNotifications.Count > 0)
        {
            var pushes = newNotifications
                .Select(item => new NotificationPush(item.Audience, item.UserId, item.MerchantId, new NotificationDto(item.Id, item.Type, item.Title, item.Body, item.RelatedType, item.RelatedId, item.IsRead, item.CreatedAt)))
                .ToList();

            await notificationPublisher.PublishAsync(pushes, cancellationToken);
        }

        return affected;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");

        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");

        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");

        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

        modelBuilder.HasSequence<int>("MerchantNumbers").StartsAt(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(IAuditable).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property(nameof(IAuditable.CreatedAt)).HasDefaultValueSql("SYSUTCDATETIME()");
            }

            var parameter = Expression.Parameter(clrType, "entity");

            Expression? filter = null;

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                filter = Expression.Equal(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)), Expression.Constant(false));
            }

            if (typeof(IMerchantOwned).IsAssignableFrom(clrType))
            {
                // entity => this.CurrentMerchantId == null || entity.MerchantId == this.CurrentMerchantId
                var current = Expression.Property(Expression.Constant(this), nameof(CurrentMerchantId));

                var owner = Expression.Convert(Expression.Property(parameter, nameof(IMerchantOwned.MerchantId)), typeof(int?));

                var tenant = Expression.OrElse(Expression.Equal(current, Expression.Constant(null, typeof(int?))), Expression.Equal(owner, current));

                filter = filter is null ? tenant : Expression.AndAlso(filter, tenant);
            }

            if (filter is not null)
            {
                modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(filter, parameter));
            }
        }
    }

    private void ApplyAuditAndSoftDelete()
    {
        var now = DateTime.UtcNow;

        var currentUserId = userAppService.UserId;

        var currentMerchantId = CurrentMerchantId;

        foreach (var entry in ChangeTracker.Entries())
        {
            GuardMerchant(entry, currentMerchantId);

            if (entry.Entity is ISearchable searchable && entry.State is EntityState.Added or EntityState.Modified)
            {
                var searchText = searchable.BuildSearchText();

                if (searchable.SearchText != searchText)
                {
                    searchable.SearchText = searchText;
                }
            }

            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable softDeletable })
            {
                entry.State = EntityState.Modified;

                softDeletable.IsDeleted = true;
            }

            if (entry.Entity is not IAuditable auditable)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = now;

                auditable.CreatedBy = currentUserId;
            }

            else if (entry.State == EntityState.Modified)
            {
                if (OnlySearchTextChanged(entry))
                {
                    continue;
                }

                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;

                entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;

                auditable.UpdatedAt = now;

                auditable.UpdatedBy = currentUserId;
            }
        }
    }

    /// <summary>Fills the merchant of new rows and stops a merchant user from writing another merchant's rows.</summary>
    private static void GuardMerchant(EntityEntry entry, int? currentMerchantId)
    {
        if (entry.Entity is not IMerchantOwned owned || entry.State is EntityState.Unchanged or EntityState.Detached)
        {
            return;
        }

        if (entry.State == EntityState.Added && owned.MerchantId == 0)
        {
            owned.MerchantId = currentMerchantId ?? throw new InvalidOperationException($"A new {entry.Metadata.ClrType.Name} needs a merchant.");
        }

        if (currentMerchantId is { } merchantId && (owned.MerchantId != merchantId || (entry.State == EntityState.Modified && entry.Property(nameof(IMerchantOwned.MerchantId)).IsModified)))
        {
            throw new ForbiddenException("This record belongs to another store.");
        }
    }

    private static readonly HashSet<Type> AuditedTypes =
    [
        typeof(Merchant),

        typeof(MerchantApplication),

        typeof(Branch),

        typeof(Register),

        typeof(Category),

        typeof(Product),

        typeof(Warehouse),

        typeof(Customer),

        typeof(Supplier),

        typeof(Promotion),

        typeof(SalesInvoice),

        typeof(SalesReturn),

        typeof(PurchaseOrder),

        typeof(CustomerBalanceEntry),

        typeof(SupplierBalanceEntry),

        typeof(User),

        typeof(StaffPermission),

        typeof(Ticket)
    ];

    private static readonly HashSet<string> IgnoredAuditProperties =
    [
        nameof(ISearchable.SearchText),

        nameof(IAuditable.CreatedAt),

        nameof(IAuditable.CreatedBy),

        nameof(IAuditable.UpdatedAt),

        nameof(IAuditable.UpdatedBy),

        nameof(ISoftDeletable.IsDeleted),

        "RowVersion",

        "ConcurrencyStamp",

        "SecurityStamp",

        "NormalizedUserName",

        "NormalizedEmail",

        "AccessFailedCount",

        "LockoutEnd",

        "LastLoginAt",

        "LastMessageAt",

        "SubscriptionReminderSentAt"
    ];

    private static readonly HashSet<string> RedactedAuditProperties =
    [
        "PasswordHash"
    ];

    private sealed record PendingAudit(EntityEntry Entry, string Action, Dictionary<string, object?>? OldValues, Dictionary<string, object?>? NewValues);

    private List<PendingAudit> CollectAuditTrail()
    {
        var pending = new List<PendingAudit>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (!AuditedTypes.Contains(entry.Entity.GetType()))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                var created = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.IsPrimaryKey() || property.Metadata.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never || IgnoredAuditProperties.Contains(property.Metadata.Name) || property.CurrentValue is null)
                    {
                        continue;
                    }

                    created[property.Metadata.Name] = AuditValue(property.Metadata.Name, property.CurrentValue);
                }

                pending.Add(new PendingAudit(entry, "Create", null, created));

                continue;
            }

            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            if (entry.Entity is ISoftDeletable { IsDeleted: true } && entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified)
            {
                pending.Add(new PendingAudit(entry, "Delete", null, null));

                continue;
            }

            var before = new Dictionary<string, object?>();

            var after = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (!property.IsModified || IgnoredAuditProperties.Contains(property.Metadata.Name) || Equals(property.OriginalValue, property.CurrentValue))
                {
                    continue;
                }

                before[property.Metadata.Name] = AuditValue(property.Metadata.Name, property.OriginalValue);

                after[property.Metadata.Name] = AuditValue(property.Metadata.Name, property.CurrentValue);
            }

            if (after.Count > 0)
            {
                pending.Add(new PendingAudit(entry, "Update", before, after));
            }
        }

        return pending;
    }

    private static bool OnlySearchTextChanged(EntityEntry entry) => entry.Entity is ISearchable && entry.Properties.Where(property => property.IsModified).All(property => property.Metadata.Name == nameof(ISearchable.SearchText));

    private static object? AuditValue(string propertyName, object? value) => RedactedAuditProperties.Contains(propertyName) ? "[redacted]" : value;

    private AuditLog CreateAuditLog(PendingAudit pending)
    {
        var entry = pending.Entry;

        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties ?? [];

        var entityId = string.Join(",", keyProperties.Select(property => entry.Property(property.Name).CurrentValue?.ToString()));

        return new AuditLog
        {
            MerchantId = ResolveMerchantId(entry),

            UserId = userAppService.UserId,

            Action = pending.Action,

            EntityName = entry.Metadata.ClrType.Name,

            EntityId = entityId.Length == 0 ? null : entityId,

            OldValues = pending.OldValues is null ? null : JsonSerializer.Serialize(pending.OldValues),

            NewValues = pending.NewValues is null ? null : JsonSerializer.Serialize(pending.NewValues),

            IpAddress = userAppService.IpAddress
        };
    }

    private int? ResolveMerchantId(EntityEntry entry) => entry.Entity switch
    {
        Merchant merchant => merchant.Id,

        IMerchantOwned owned => owned.MerchantId,

        User user => user.MerchantId,

        Ticket ticket => ticket.MerchantId,

        MerchantApplication application => application.MerchantId,

        _ => userAppService.MerchantId
    };
}
