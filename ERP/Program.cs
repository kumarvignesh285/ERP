using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Models;
using ERP.Interfaces;
using ERP.Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllersWithViews()
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
        // Apply pending migrations automatically
        context.Database.Migrate();
        
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
        END";
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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
