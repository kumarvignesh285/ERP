using System.Security.Claims;
using ERP.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ERP.Services;

public class CompanyContext : ICompanyContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private int? _overrideCompanyId;
    private string? _overrideCompanyCode;
    private string? _overrideCompanyName;
    private bool _hasOverride;

    public CompanyContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? CurrentCompanyId
    {
        get
        {
            if (_hasOverride)
                return _overrideCompanyId;

            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                return null;

            // If Super Admin, read active company from server session
            if (IsSuperAdmin)
            {
                var sessionCompanyId = httpContext?.Session.GetInt32("ActiveCompanyId");
                if (sessionCompanyId.HasValue && sessionCompanyId.Value > 0)
                {
                    return sessionCompanyId.Value;
                }
                return null; // System mode (All Companies / No active company selected)
            }

            // For Company Admin & Company Users, tenant is strictly determined by server-issued claims
            var claimVal = user.FindFirst("CompanyId")?.Value;
            if (int.TryParse(claimVal, out var cid) && cid > 0)
                return cid;

            return null;
        }
    }

    public string? CurrentCompanyCode
    {
        get
        {
            if (_hasOverride)
                return _overrideCompanyCode;

            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                return null;

            if (IsSuperAdmin)
            {
                return httpContext?.Session.GetString("ActiveCompanyCode");
            }

            return user.FindFirst("CompanyCode")?.Value;
        }
    }

    public string? CurrentCompanyName
    {
        get
        {
            if (_hasOverride)
                return _overrideCompanyName;

            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                return null;

            if (IsSuperAdmin)
            {
                return httpContext?.Session.GetString("ActiveCompanyName");
            }

            return user.FindFirst("CompanyName")?.Value;
        }
    }

    public int? CompanyId => CurrentCompanyId;
    public string? CompanyCode => CurrentCompanyCode;
    public string? CompanyName => CurrentCompanyName;
    public bool HasActiveCompany => CurrentCompanyId.HasValue && CurrentCompanyId.Value > 0;
    public bool HasCompanyContext => HasActiveCompany;

    public bool IsSuperAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                return false;

            return user.IsInRole("Super Admin") ||
                   string.Equals(user.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public void SetCompanyOverride(int? companyId, string? companyCode, string? companyName)
    {
        _overrideCompanyId = companyId;
        _overrideCompanyCode = companyCode;
        _overrideCompanyName = companyName;
        _hasOverride = true;
    }
}
