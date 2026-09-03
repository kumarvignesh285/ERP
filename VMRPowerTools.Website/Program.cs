using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Application.Interfaces;
using VMRPowerTools.Infrastructure.Data;
using VMRPowerTools.Infrastructure.Repositories;
using VMRPowerTools.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Connection Settings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<WebsiteDbContext>(options =>
    options.UseSqlServer(connectionString));

// ASP.NET Core Identity (Sharing User DB with ERP)
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<WebsiteDbContext>()
.AddDefaultTokenProviders();

// Configure Cookie Settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Dependency Injection: Register Generic Repositories & Unit of Work
builder.Services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));

// Dependency Injection: Register Custom Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();

// Dependency Injection: Register Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();

// Enable Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Seed Database with Premium Industrial Catalog Items
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<VMRPowerTools.Infrastructure.Data.WebsiteDbContext>();
        await VMRPowerTools.Infrastructure.Data.DatabaseSeeder.ReseedAsync(dbContext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database on website startup.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use Response Compression
app.UseResponseCompression();

// Add Global Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer-when-downgrade");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self' https:; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://unpkg.com https://cdn.tailwindcss.com https://code.jquery.com; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://cdn.tailwindcss.com; font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; img-src 'self' data: https://images.unsplash.com https://cdn.jsdelivr.net https://unpkg.com; frame-src 'self' https://www.google.com;");
    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
