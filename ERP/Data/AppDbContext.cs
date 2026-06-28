using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ERP.Models;

namespace ERP.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

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
    }
}
