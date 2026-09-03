using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Models;
using ERP.Interfaces;
using ERP.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

// Add Connection String & DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure Application Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Session State for Super Admin Active Company Context
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Multi-Company Tenant & Claims Infrastructure
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICompanyContext, CompanyContext>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppClaimsPrincipalFactory>();
builder.Services.AddScoped<ICompanyProvisioningService, CompanyProvisioningService>();
builder.Services.AddScoped<ICompanySampleDataService, CompanySampleDataService>();
builder.Services.AddScoped<ILoginHistoryService, LoginHistoryService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Register ERP Core Services
builder.Services.AddScoped<IPdfProductParserService, PdfProductParserService>();
builder.Services.AddScoped<IMasterService, MasterService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<ICRMService, CRMService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Add Razor Pages and Controllers with Views
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Account");
});
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ERP.Filters.ErpExceptionFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        try
        {
            // Apply pending migrations automatically
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Could not run context.Database.Migrate(). Continuing database startup sequence.");
        }
        
        // Forcefully add missing columns if they were somehow dropped despite migrations showing as applied
        var sql = @"
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'BillFooterNote')
        BEGIN
            ALTER TABLE [Companies] ADD [BillFooterNote] nvarchar(500) NULL;
            ALTER TABLE [Companies] ADD [BillType] nvarchar(50) NOT NULL DEFAULT N'Tax Invoice';
            ALTER TABLE [Companies] ADD [PurchaseBillNextNumber] int NOT NULL DEFAULT 1;
            ALTER TABLE [Companies] ADD [PurchaseBillPrefix] nvarchar(20) NOT NULL DEFAULT N'PINV-';
            ALTER TABLE [Companies] ADD [PurchaseBillStartNumber] int NOT NULL DEFAULT 1;
            ALTER TABLE [Companies] ADD [SalesBillNextNumber] int NOT NULL DEFAULT 1;
            ALTER TABLE [Companies] ADD [SalesBillPrefix] nvarchar(20) NOT NULL DEFAULT N'INV-';
            ALTER TABLE [Companies] ADD [SalesBillStartNumber] int NOT NULL DEFAULT 1;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Companies]') AND name = 'AlternatePhone')
            ALTER TABLE [Companies] ADD [AlternatePhone] nvarchar(20) NULL;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Companies]') AND name = 'BusinessType')
            ALTER TABLE [Companies] ADD [BusinessType] nvarchar(100) NULL;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Companies]') AND name = 'Currency')
            ALTER TABLE [Companies] ADD [Currency] nvarchar(10) NOT NULL DEFAULT N'INR';
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Companies]') AND name = 'FinancialYear')
            ALTER TABLE [Companies] ADD [FinancialYear] nvarchar(20) NULL;

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[AspNetUsers]') AND name = 'CompanyId')
            ALTER TABLE [AspNetUsers] ADD [CompanyId] INT NULL;
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ScreenPermissions]') AND name = 'CompanyId')
            ALTER TABLE [ScreenPermissions] ADD [CompanyId] INT NULL;

        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoginHistories')
        BEGIN
            CREATE TABLE [LoginHistories] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [UserId] NVARCHAR(450) NULL,
                [Username] NVARCHAR(100) NOT NULL,
                [Role] NVARCHAR(50) NULL,
                [CompanyId] INT NULL,
                [CompanyCode] NVARCHAR(20) NULL,
                [SessionId] NVARCHAR(100) NULL,
                [IPAddress] NVARCHAR(50) NULL,
                [UserAgent] NVARCHAR(500) NULL,
                [Browser] NVARCHAR(100) NULL,
                [Device] NVARCHAR(100) NULL,
                [OperatingSystem] NVARCHAR(100) NULL,
                [Status] NVARCHAR(50) NOT NULL,
                [FailureReason] NVARCHAR(200) NULL,
                [LoginTime] DATETIME2 NOT NULL,
                [LogoutTime] DATETIME2 NULL
            );
        END;

        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserActivityLogs')
        BEGIN
            CREATE TABLE [UserActivityLogs] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [UserId] NVARCHAR(450) NULL,
                [Username] NVARCHAR(100) NOT NULL,
                [Role] NVARCHAR(50) NULL,
                [ActivityType] NVARCHAR(50) NOT NULL,
                [PreviousCompanyId] INT NULL,
                [PreviousCompanyCode] NVARCHAR(20) NULL,
                [NewCompanyId] INT NULL,
                [NewCompanyCode] NVARCHAR(20) NULL,
                [Description] NVARCHAR(500) NULL,
                [IPAddress] NVARCHAR(50) NULL,
                [Timestamp] DATETIME2 NOT NULL
            );
        END;

        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
        BEGIN
            CREATE TABLE [AuditLogs] (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [CompanyId] INT NULL,
                [UserId] NVARCHAR(450) NULL,
                [UserName] NVARCHAR(100) NOT NULL,
                [Action] NVARCHAR(50) NOT NULL,
                [Module] NVARCHAR(50) NOT NULL,
                [EntityName] NVARCHAR(100) NULL,
                [EntityId] NVARCHAR(100) NULL,
                [Description] NVARCHAR(1000) NULL,
                [OldValues] NVARCHAR(MAX) NULL,
                [NewValues] NVARCHAR(MAX) NULL,
                [IpAddress] NVARCHAR(50) NULL,
                [UserAgent] NVARCHAR(500) NULL,
                [RequestPath] NVARCHAR(500) NULL,
                [HttpMethod] NVARCHAR(10) NULL,
                [Status] NVARCHAR(50) NOT NULL DEFAULT N'Success',
                [Severity] NVARCHAR(20) NOT NULL DEFAULT N'Info',
                [CorrelationId] NVARCHAR(100) NULL,
                [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );

            CREATE INDEX [IX_AuditLogs_CompanyId] ON [AuditLogs]([CompanyId]);
            CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs]([UserId]);
            CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs]([Timestamp]);
            CREATE INDEX [IX_AuditLogs_EntityName] ON [AuditLogs]([EntityName]);
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AuditLogs') AND name = 'Severity')
                ALTER TABLE [AuditLogs] ADD [Severity] NVARCHAR(20) NOT NULL DEFAULT N'Info';
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AuditLogs') AND name = 'CorrelationId')
                ALTER TABLE [AuditLogs] ADD [CorrelationId] NVARCHAR(100) NULL;
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AuditLogs') AND name = 'Status')
                ALTER TABLE [AuditLogs] ADD [Status] NVARCHAR(50) NOT NULL DEFAULT N'Success';
        END;

        DECLARE @Tables TABLE (TableName NVARCHAR(100));
        INSERT INTO @Tables VALUES 
        ('AccountGroups'), ('Banks'), ('Brands'), ('Categories'), ('Customers'),
        ('DeliveryChallans'), ('Employees'), ('GoodsReceiptNotes'), ('Ledgers'), ('PaymentModes'),
        ('Products'), ('PurchaseInvoices'), ('PurchaseOrders'), ('PurchaseReturns'), ('SalesInvoices'),
        ('SalesOrders'), ('SalesQuotations'), ('SalesReturns'), ('StockAdjustments'), ('StockTransactions'),
        ('StockTransfers'), ('PhysicalStockVerifications'), ('Suppliers'), ('Taxes'), ('Units'),
        ('Vouchers'), ('Warehouses'), ('Leads'), ('FollowUps'), ('Opportunities');

        DECLARE @tbl NVARCHAR(100);
        DECLARE cur CURSOR FOR SELECT TableName FROM @Tables;
        OPEN cur;
        FETCH NEXT FROM cur INTO @tbl;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @cmd NVARCHAR(MAX) = N'IF EXISTS (SELECT * FROM sys.tables WHERE name = ''' + @tbl + ''')
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''' + @tbl + ''') AND name = ''CompanyId'')
                BEGIN
                    ALTER TABLE [' + @tbl + '] ADD [CompanyId] INT NOT NULL DEFAULT 1;
                END
            END';
            EXEC sp_executesql @cmd;
            FETCH NEXT FROM cur INTO @tbl;
        END
        CLOSE cur;
        DEALLOCATE cur;
        ";
        context.Database.ExecuteSqlRaw(sql);

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Enable detailed developer exceptions in all environments to aid local network deployment troubleshooting
app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name != "HostAbortedException")
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
