using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Services;

public class PermissionService : IPermissionService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public PermissionService(UserManager<AppUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string screenName, string action)
    {
        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Super Admin bypasses all authorization rules
        if (user.IsInRole("Super Admin"))
        {
            return true;
        }

        // Company Management and System Settings are restricted to Super Admin
        if (string.Equals(screenName, "Company Management", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(screenName, "System Settings", StringComparison.OrdinalIgnoreCase))
        {
            return user.IsInRole("Admin");
        }

        // Company Admin has full access within their assigned company
        if (user.IsInRole("Company Admin") || user.IsInRole("CompanyAdmin") || user.IsInRole("Admin"))
        {
            return true;
        }

        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null && !string.IsNullOrEmpty(user.Identity?.Name))
        {
            appUser = await _userManager.FindByNameAsync(user.Identity.Name) ?? await _userManager.FindByEmailAsync(user.Identity.Name);
        }

        if (appUser == null)
        {
            return false;
        }

        // Check explicit permissions configured for this user and screen
        var permission = await _context.ScreenPermissions
            .FirstOrDefaultAsync(sp => sp.IsActive && sp.UserId == appUser.Id && sp.ScreenName == screenName);

        var normalizedAction = (action ?? string.Empty).ToLowerInvariant();

        if (permission != null)
        {
            return normalizedAction switch
            {
                "view" or "read" or "print" or "export" => permission.CanView,
                "edit" or "create" or "update" or "save" => permission.CanEdit,
                "delete" or "remove" => permission.CanDelete,
                _ => false
            };
        }

        // Role-based fallback if individual screen permission records are not yet generated
        var roles = await _userManager.GetRolesAsync(appUser);
        if (roles.Contains("Viewer"))
        {
            return normalizedAction is "view" or "read" or "print" or "export";
        }

        if (roles.Contains("Manager"))
        {
            if (string.Equals(screenName, "User Management", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(screenName, "Role Configuration", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedAction is "view" or "read";
            }
            return true;
        }

        if (roles.Contains("Sales User") || roles.Contains("Sales"))
        {
            var isSalesOrCrm = screenName is "Quotation" or "Sales Order" or "Delivery Challan" or "Sales Invoice" or "Sales Return"
                or "Leads" or "Follow Ups" or "Opportunities" or "Pipeline View" or "Customer Master";
            if (isSalesOrCrm)
            {
                return normalizedAction is not ("delete" or "remove");
            }
            if (screenName is "Product Master" or "Category Master" or "Stock Opening" or "Sales Reports")
            {
                return normalizedAction is "view" or "read" or "print" or "export";
            }
        }

        if (roles.Contains("Purchase User") || roles.Contains("Purchase"))
        {
            var isPurchase = screenName is "Purchase Order" or "Goods Receipt Note" or "Purchase Invoice" or "Purchase Return"
                or "Supplier Master" or "Product Master";
            if (isPurchase)
            {
                return normalizedAction is not ("delete" or "remove");
            }
            if (screenName is "Stock Opening" or "Stock Transfer" or "Purchase Reports")
            {
                return normalizedAction is "view" or "read" or "print" or "export";
            }
        }

        if (roles.Contains("Accountant"))
        {
            var isAccount = screenName is "Receipt Voucher" or "Payment Voucher" or "Contra Voucher" or "Journal Voucher"
                or "Debit Note" or "Credit Note" or "Cash Book" or "Bank Book" or "Accounting Reports"
                or "Ledger Master" or "Bank Master" or "Tax Settings";
            if (isAccount)
            {
                return normalizedAction is not ("delete" or "remove");
            }
        }

        if (roles.Contains("Employee"))
        {
            return normalizedAction is "view" or "read";
        }

        return false;
    }
}
