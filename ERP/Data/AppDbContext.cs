using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    private readonly ICompanyContext? _companyContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICompanyContext? companyContext = null) : base(options)
    {
        _companyContext = companyContext;
    }

    public int? CurrentCompanyId => _companyContext?.CurrentCompanyId;
    public int ActiveTenantId => _companyContext?.CurrentCompanyId ?? 0;
    public bool IsSuperAdmin => _companyContext?.IsSuperAdmin ?? false;

    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Brand> Brands { get; set; } = null!;
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<AccountGroup> AccountGroups { get; set; } = null!;
    public DbSet<Ledger> Ledgers { get; set; } = null!;
    public DbSet<Bank> Banks { get; set; } = null!;
    public DbSet<Tax> Taxes { get; set; } = null!;
    public DbSet<PaymentMode> PaymentModes { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;

    // Sales
    public DbSet<SalesQuotation> SalesQuotations { get; set; } = null!;
    public DbSet<SalesQuotationItem> SalesQuotationItems { get; set; } = null!;
    public DbSet<SalesOrder> SalesOrders { get; set; } = null!;
    public DbSet<SalesOrderItem> SalesOrderItems { get; set; } = null!;
    public DbSet<DeliveryChallan> DeliveryChallans { get; set; } = null!;
    public DbSet<DeliveryChallanItem> DeliveryChallanItems { get; set; } = null!;
    public DbSet<SalesInvoice> SalesInvoices { get; set; } = null!;
    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; } = null!;
    public DbSet<SalesReturn> SalesReturns { get; set; } = null!;
    public DbSet<SalesReturnItem> SalesReturnItems { get; set; } = null!;

    // Purchase
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes { get; set; } = null!;
    public DbSet<GoodsReceiptNoteItem> GoodsReceiptNoteItems { get; set; } = null!;
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; } = null!;
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = null!;
    public DbSet<PurchaseReturn> PurchaseReturns { get; set; } = null!;
    public DbSet<PurchaseReturnItem> PurchaseReturnItems { get; set; } = null!;

    // Inventory
    public DbSet<StockTransaction> StockTransactions { get; set; } = null!;
    public DbSet<StockTransfer> StockTransfers { get; set; } = null!;
    public DbSet<StockTransferItem> StockTransferItems { get; set; } = null!;
    public DbSet<StockAdjustment> StockAdjustments { get; set; } = null!;
    public DbSet<StockAdjustmentItem> StockAdjustmentItems { get; set; } = null!;
    public DbSet<PhysicalStockVerification> PhysicalStockVerifications { get; set; } = null!;
    public DbSet<PhysicalStockVerificationItem> PhysicalStockVerificationItems { get; set; } = null!;

    // Accounts
    public DbSet<Voucher> Vouchers { get; set; } = null!;
    public DbSet<VoucherItem> VoucherItems { get; set; } = null!;

    // CRM
    public DbSet<Lead> Leads { get; set; } = null!;
    public DbSet<FollowUp> FollowUps { get; set; } = null!;
    public DbSet<Opportunity> Opportunities { get; set; } = null!;

    // Notifications
    public DbSet<Notification> Notifications { get; set; } = null!;

    // Permissions
    public DbSet<ScreenPermission> ScreenPermissions { get; set; } = null!;

    // Audit & Security
    public DbSet<LoginHistory> LoginHistories { get; set; } = null!;
    public DbSet<UserActivityLog> UserActivityLogs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Turn off cascade delete globally or selectively to prevent multiple cascade paths
        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Specific configurations for entity fields to avoid mapping precision errors
        builder.Entity<SalesInvoiceItem>()
            .HasOne(x => x.SalesInvoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SalesQuotationItem>()
            .HasOne(x => x.SalesQuotation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesQuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SalesOrderItem>()
            .HasOne(x => x.SalesOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DeliveryChallanItem>()
            .HasOne(x => x.DeliveryChallan)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.DeliveryChallanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SalesReturnItem>()
            .HasOne(x => x.SalesReturn)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PurchaseOrderItem>()
            .HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GoodsReceiptNoteItem>()
            .HasOne(x => x.GoodsReceiptNote)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PurchaseInvoiceItem>()
            .HasOne(x => x.PurchaseInvoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PurchaseReturnItem>()
            .HasOne(x => x.PurchaseReturn)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StockTransferItem>()
            .HasOne(x => x.StockTransfer)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.StockTransferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StockAdjustmentItem>()
            .HasOne(x => x.StockAdjustment)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.StockAdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PhysicalStockVerificationItem>()
            .HasOne(x => x.PhysicalStockVerification)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PhysicalStockVerificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<VoucherItem>()
            .HasOne(x => x.Voucher)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Multi-Company Foundation Configuration ---

        // Unique Company Code
        builder.Entity<Company>()
            .HasIndex(c => c.CompanyCode)
            .IsUnique();

        // Frequently queried tenant-aware composite indexes
        builder.Entity<Product>().HasIndex(x => new { x.CompanyId, x.ProductCode });
        builder.Entity<Customer>().HasIndex(x => new { x.CompanyId, x.CustomerCode });
        builder.Entity<Supplier>().HasIndex(x => new { x.CompanyId, x.SupplierCode });
        builder.Entity<SalesInvoice>().HasIndex(x => new { x.CompanyId, x.InvoiceNumber });
        builder.Entity<PurchaseInvoice>().HasIndex(x => new { x.CompanyId, x.InvoiceNumber });
        builder.Entity<SalesOrder>().HasIndex(x => new { x.CompanyId, x.OrderNumber });
        builder.Entity<PurchaseOrder>().HasIndex(x => new { x.CompanyId, x.OrderNumber });
        builder.Entity<Voucher>().HasIndex(x => new { x.CompanyId, x.VoucherNumber });

        // Global Query Filters for company data isolation
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ICompanyOwned).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureCompanyFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { builder });
            }
        }

        builder.Entity<Notification>().HasQueryFilter(e => (IsSuperAdmin && ActiveTenantId == 0) || e.CompanyId == null || e.CompanyId == ActiveTenantId);
        builder.Entity<ScreenPermission>().HasQueryFilter(e => (IsSuperAdmin && ActiveTenantId == 0) || e.CompanyId == null || e.CompanyId == ActiveTenantId);

        builder.Entity<LoginHistory>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.LoginTime);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SessionId);
        });

        builder.Entity<UserActivityLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ActivityType);
            entity.HasIndex(e => e.Timestamp);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Module);
            entity.HasIndex(e => e.EntityName);
            entity.HasQueryFilter(e => (IsSuperAdmin && ActiveTenantId == 0) || e.CompanyId == ActiveTenantId);
        });
    }

    private void ConfigureCompanyFilter<TEntity>(ModelBuilder builder) where TEntity : class, ICompanyOwned
    {
        builder.Entity<TEntity>().HasQueryFilter(e => (IsSuperAdmin && ActiveTenantId == 0) || e.CompanyId == ActiveTenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantId();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyTenantId();
        return base.SaveChanges();
    }

    private void ApplyTenantId()
    {
        var currentCompanyId = CurrentCompanyId;
        if (currentCompanyId.HasValue && currentCompanyId.Value > 0)
        {
            foreach (var entry in ChangeTracker.Entries<ICompanyOwned>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Unconditionally force server-resolved CompanyId to prevent spoofing
                    entry.Entity.CompanyId = currentCompanyId.Value;
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (!IsSuperAdmin)
                    {
                        // Prevent changing the company assignment of an existing record
                        entry.Property(x => x.CompanyId).IsModified = false;

                        var originalCompanyId = entry.Property(x => x.CompanyId).OriginalValue;
                        if (originalCompanyId != currentCompanyId.Value)
                        {
                            throw new UnauthorizedAccessException("Tenant security violation: You do not have permission to modify records belonging to another company.");
                        }
                    }
                }
                else if (entry.State == EntityState.Deleted)
                {
                    if (!IsSuperAdmin)
                    {
                        var originalCompanyId = entry.Property(x => x.CompanyId).OriginalValue;
                        if (originalCompanyId != currentCompanyId.Value)
                        {
                            throw new UnauthorizedAccessException("Tenant security violation: You do not have permission to delete records belonging to another company.");
                        }
                    }
                }
            }

            foreach (var entry in ChangeTracker.Entries<Notification>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CompanyId = currentCompanyId.Value;
                }
            }

            foreach (var entry in ChangeTracker.Entries<ScreenPermission>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CompanyId = currentCompanyId.Value;
                }
            }
        }
    }
}
