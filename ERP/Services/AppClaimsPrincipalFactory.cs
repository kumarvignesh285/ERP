using System.Security.Claims;
using ERP.Data;
using ERP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Services;

public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    private readonly AppDbContext _context;

    public AppClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        AppDbContext context)
        : base(userManager, roleManager, optionsAccessor)
    {
        _context = context;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrEmpty(user.FullName))
        {
            identity.AddClaim(new Claim("FullName", user.FullName));
        }

        var isSuperAdmin = await UserManager.IsInRoleAsync(user, "Super Admin");
        if (isSuperAdmin)
        {
            identity.AddClaim(new Claim("IsSuperAdmin", "true"));
        }

        if (user.CompanyId.HasValue && user.CompanyId.Value > 0)
        {
            identity.AddClaim(new Claim("CompanyId", user.CompanyId.Value.ToString()));

            // Retrieve company details using IgnoreQueryFilters to bypass any active tenant filter
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == user.CompanyId.Value);

            if (company != null)
            {
                if (!string.IsNullOrEmpty(company.CompanyCode))
                {
                    identity.AddClaim(new Claim("CompanyCode", company.CompanyCode));
                }
                if (!string.IsNullOrEmpty(company.CompanyName))
                {
                    identity.AddClaim(new Claim("CompanyName", company.CompanyName));
                }
            }
        }

        return identity;
    }
}
