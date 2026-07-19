using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Domain.Entities;

namespace VMRPowerTools.Infrastructure.Data;

public class WebsiteDbContext : IdentityDbContext<AppUser>
{
    public WebsiteDbContext(DbContextOptions<WebsiteDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Brand> Brands { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Lead> Leads { get; set; } = null!;
    public DbSet<FollowUp> FollowUps { get; set; } = null!;
    
    // ERP Sales & Invoicing Mappings
    public DbSet<SalesOrder> SalesOrders { get; set; } = null!;
    public DbSet<SalesOrderItem> SalesOrderItems { get; set; } = null!;
    public DbSet<SalesInvoice> SalesInvoices { get; set; } = null!;
    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; } = null!;
    public DbSet<StockTransaction> StockTransactions { get; set; } = null!;

    // ERP Accounting/Ledger Mappings
    public DbSet<AccountGroup> AccountGroups { get; set; } = null!;
    public DbSet<Ledger> Ledgers { get; set; } = null!;
    public DbSet<Voucher> Vouchers { get; set; } = null!;
    public DbSet<VoucherItem> VoucherItems { get; set; } = null!;

    // Website CMS Mappings
    public DbSet<CmsSetting> CmsSettings { get; set; } = null!;
    public DbSet<CmsBlogPost> CmsBlogPosts { get; set; } = null!;
    public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Turn off cascade delete globally except for specific parent-child relationships
        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Configure Cascade Deletes
        builder.Entity<FollowUp>()
            .HasOne(f => f.Lead)
            .WithMany(l => l.FollowUps)
            .HasForeignKey(f => f.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SalesOrderItem>()
            .HasOne(x => x.SalesOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SalesInvoiceItem>()
            .HasOne(x => x.SalesInvoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<VoucherItem>()
            .HasOne(x => x.Voucher)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Cascade);

        // Specify Column Types for Decimals to avoid warnings/precision errors
        builder.Entity<Product>().Property(p => p.PurchasePrice).HasPrecision(18, 2);
        builder.Entity<Product>().Property(p => p.SalesPrice).HasPrecision(18, 2);
        builder.Entity<Product>().Property(p => p.MRP).HasPrecision(18, 2);
        builder.Entity<Product>().Property(p => p.Discount).HasPrecision(18, 2);
        builder.Entity<Product>().Property(p => p.GSTPercentage).HasPrecision(18, 2);
        builder.Entity<Product>().Property(p => p.OpeningStock).HasPrecision(18, 4);
        builder.Entity<Product>().Property(p => p.CurrentStock).HasPrecision(18, 4);
        builder.Entity<Product>().Property(p => p.MinimumStock).HasPrecision(18, 4);
        builder.Entity<Product>().Property(p => p.MaximumStock).HasPrecision(18, 4);
        builder.Entity<Product>().Property(p => p.ReorderLevel).HasPrecision(18, 4);

        builder.Entity<Customer>().Property(c => c.CreditLimit).HasPrecision(18, 2);
        builder.Entity<Customer>().Property(c => c.OpeningBalance).HasPrecision(18, 2);

        // SalesOrder
        builder.Entity<SalesOrder>().Property(o => o.SubTotal).HasPrecision(18, 2);
        builder.Entity<SalesOrder>().Property(o => o.TaxAmount).HasPrecision(18, 2);
        builder.Entity<SalesOrder>().Property(o => o.DiscountAmount).HasPrecision(18, 2);
        builder.Entity<SalesOrder>().Property(o => o.GrandTotal).HasPrecision(18, 2);

        // SalesOrderItem
        builder.Entity<SalesOrderItem>().Property(i => i.Quantity).HasPrecision(18, 4);
        builder.Entity<SalesOrderItem>().Property(i => i.DeliveredQuantity).HasPrecision(18, 4);
        builder.Entity<SalesOrderItem>().Property(i => i.Rate).HasPrecision(18, 2);
        builder.Entity<SalesOrderItem>().Property(i => i.Discount).HasPrecision(18, 2);
        builder.Entity<SalesOrderItem>().Property(i => i.TaxPercentage).HasPrecision(18, 2);
        builder.Entity<SalesOrderItem>().Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Entity<SalesOrderItem>().Property(i => i.Amount).HasPrecision(18, 2);

        // SalesInvoice
        builder.Entity<SalesInvoice>().Property(i => i.SubTotal).HasPrecision(18, 2);
        builder.Entity<SalesInvoice>().Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoice>().Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoice>().Property(i => i.RoundOff).HasPrecision(18, 2);
        builder.Entity<SalesInvoice>().Property(i => i.GrandTotal).HasPrecision(18, 2);
        builder.Entity<SalesInvoice>().Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoice>().Property(i => i.BalanceAmount).HasPrecision(18, 2);

        // SalesInvoiceItem
        builder.Entity<SalesInvoiceItem>().Property(i => i.Quantity).HasPrecision(18, 4);
        builder.Entity<SalesInvoiceItem>().Property(i => i.Rate).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceItem>().Property(i => i.Discount).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceItem>().Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceItem>().Property(i => i.TaxPercentage).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceItem>().Property(i => i.CGSTAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceItem>().Property(i => i.SGSTAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceItem>().Property(i => i.IGSTAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceItem>().Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceItem>().Property(i => i.Amount).HasPrecision(18, 2);

        // StockTransaction
        builder.Entity<StockTransaction>().Property(t => t.Quantity).HasPrecision(18, 4);
        builder.Entity<StockTransaction>().Property(t => t.Rate).HasPrecision(18, 2);

        // Accounting/Ledger dec configurations
        builder.Entity<Ledger>().Property(l => l.OpeningBalance).HasPrecision(18, 2);
        builder.Entity<Voucher>().Property(v => v.TotalAmount).HasPrecision(18, 2);
        builder.Entity<VoucherItem>().Property(vi => vi.DebitAmount).HasPrecision(18, 2);
        builder.Entity<VoucherItem>().Property(vi => vi.CreditAmount).HasPrecision(18, 2);
    }
}
