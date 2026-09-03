using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Models;

namespace ERP.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // 1. Roles
        string[] roles = { "Super Admin", "Admin", "Company Admin", "CompanyAdmin", "CompanyUser", "Manager", "Employee", "Accountant", "Sales", "Sales User", "Purchase", "Purchase User", "Viewer" };
        foreach (var r in roles)
        {
            if (!await roleManager.RoleExistsAsync(r))
            {
                await roleManager.CreateAsync(new IdentityRole(r));
            }
        }

        // 2. Default Super Admin User
        var adminEmail = "admin@smarterp.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrator",
                Mobile = "9876543210",
                EmailConfirmed = true,
                ClearTextPassword = "Admin@123",
                CompanyId = null,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Super Admin");
            }
        }
        else
        {
            if (string.IsNullOrEmpty(adminUser.ClearTextPassword))
            {
                adminUser.ClearTextPassword = "Admin@123";
            }
            adminUser.IsActive = true;
            adminUser.CompanyId = null;
            await userManager.UpdateAsync(adminUser);

            if (!await userManager.IsInRoleAsync(adminUser, "Super Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Super Admin");
            }
        }

        // 3. Default Screen Permissions for Super Admin
        if (adminUser != null && !context.ScreenPermissions.Any(sp => sp.UserId == adminUser.Id))
        {
            var defaultPermissions = new List<ScreenPermission>();
            var allScreens = new[]
            {
                // Masters
                "Company Master", "Customer Master", "Supplier Master", "Product Master", "Category Master", "Brand Master", "Unit Master", "Warehouse Master", "Ledger Master", "Employee Master", "Account Groups", "Bank Master", "Tax Settings", "Payment Modes",
                // Sales
                "Quotation", "Sales Order", "Delivery Challan", "Sales Invoice", "Sales Return",
                // Purchase
                "Purchase Order", "Goods Receipt Note", "Purchase Invoice", "Purchase Return",
                // Inventory
                "Stock Opening", "Stock Transfer", "Stock Adjustment", "Physical Stock",
                // Accounts
                "Receipt Voucher", "Payment Voucher", "Contra Voucher", "Journal Voucher", "Debit Note", "Credit Note", "Cash Book", "Bank Book",
                // CRM
                "Leads", "Follow Ups", "Opportunities", "Pipeline View",
                // Reports
                "Sales Reports", "Purchase Reports", "Inventory Reports", "Accounting Reports",
                // Settings
                "User Management", "Role Configuration", "Company Setup", "System Settings"
            };

            foreach (var screen in allScreens)
            {
                defaultPermissions.Add(new ScreenPermission
                {
                    UserId = adminUser.Id,
                    ScreenName = screen,
                    CanView = true,
                    CanEdit = true,
                    CanDelete = true
                });
            }

            context.ScreenPermissions.AddRange(defaultPermissions);
            await context.SaveChangesAsync();
        }
    }
}
