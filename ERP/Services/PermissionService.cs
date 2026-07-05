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
        if (user == null || !user.Identity.IsAuthenticated)
        {
            return false;
        }

        // Super Admin bypasses all authorization rules
        if (user.IsInRole("Super Admin"))
        {
            return true;
        }

        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null)
        {
            return false;
        }

        // Retrieve permissions for this specific user and screen
        var permission = await _context.ScreenPermissions
            .FirstOrDefaultAsync(sp => sp.IsActive && sp.UserId == appUser.Id && sp.ScreenName == screenName);

        if (permission == null)
        {
            return false;
        }

        return action.ToLowerInvariant() switch
        {
            "view" => permission.CanView,
            "edit" => permission.CanEdit,
            "delete" => permission.CanDelete,
            _ => false
        };
    }
}
