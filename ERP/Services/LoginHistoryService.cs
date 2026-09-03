using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Data;
using ERP.Helpers;
using ERP.Interfaces;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Services;

public class LoginHistoryService : ILoginHistoryService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<LoginHistoryService> _logger;

    public LoginHistoryService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger<LoginHistoryService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task RecordSuccessfulLoginAsync(AppUser user, string sessionId, string? ipAddress, string? userAgent)
    {
        try
        {
            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "Super Admin");
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = isSuperAdmin ? "Super Admin" : (roles.FirstOrDefault() ?? "CompanyUser");

            int? companyId = null;
            string? companyCode = null;

            if (!isSuperAdmin && user.CompanyId.HasValue && user.CompanyId.Value > 0)
            {
                companyId = user.CompanyId.Value;
                var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId.Value);
                companyCode = company?.CompanyCode;
            }

            var (browser, os, device) = UserAgentHelper.Parse(userAgent);

            var history = new LoginHistory
            {
                UserId = user.Id,
                Username = user.UserName ?? user.Email ?? "Unknown",
                Role = primaryRole,
                CompanyId = companyId,
                CompanyCode = companyCode,
                LoginTime = DateTime.UtcNow,
                Status = "Success",
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Browser = browser,
                OperatingSystem = os,
                Device = device,
                SessionId = sessionId
            };

            _context.LoginHistories.Add(history);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording successful login for user {Username}.", user?.UserName);
        }
    }

    public async Task RecordFailedLoginAsync(string username, string reason, string? ipAddress, string? userAgent, int? companyId = null, string? companyCode = null)
    {
        try
        {
            // If companyId was not explicitly passed, inspect if user exists to identify company
            if (!companyId.HasValue && !string.IsNullOrWhiteSpace(username))
            {
                var user = await _userManager.FindByNameAsync(username.Trim()) ?? await _userManager.FindByEmailAsync(username.Trim());
                if (user != null && user.CompanyId.HasValue)
                {
                    companyId = user.CompanyId;
                    var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId.Value);
                    companyCode = company?.CompanyCode;
                }
            }

            var (browser, os, device) = UserAgentHelper.Parse(userAgent);

            var history = new LoginHistory
            {
                UserId = null,
                Username = string.IsNullOrWhiteSpace(username) ? "Anonymous" : username.Trim(),
                Role = null,
                CompanyId = companyId,
                CompanyCode = companyCode,
                LoginTime = DateTime.UtcNow,
                Status = "Failed",
                FailureReason = reason,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Browser = browser,
                OperatingSystem = os,
                Device = device
            };

            _context.LoginHistories.Add(history);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording failed login for username {Username}.", username);
        }
    }

    public async Task RecordLogoutAsync(string? userId, string? sessionId)
    {
        try
        {
            LoginHistory? record = null;

            if (!string.IsNullOrEmpty(sessionId))
            {
                record = await _context.LoginHistories
                    .IgnoreQueryFilters()
                    .Where(h => h.SessionId == sessionId && h.Status == "Success")
                    .OrderByDescending(h => h.LoginTime)
                    .FirstOrDefaultAsync();
            }

            if (record == null && !string.IsNullOrEmpty(userId))
            {
                record = await _context.LoginHistories
                    .IgnoreQueryFilters()
                    .Where(h => h.UserId == userId && h.Status == "Success")
                    .OrderByDescending(h => h.LoginTime)
                    .FirstOrDefaultAsync();
            }

            if (record != null)
            {
                record.LogoutTime = DateTime.UtcNow;
                record.Status = "LoggedOut";
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording logout for userId {UserId}.", userId);
        }
    }

    public async Task RecordCompanySwitchAsync(string userId, string username, int? prevCompanyId, string? prevCompanyCode, int? newCompanyId, string? newCompanyCode, string? ipAddress)
    {
        try
        {
            var prevText = string.IsNullOrEmpty(prevCompanyCode) ? "System View (All)" : prevCompanyCode;
            var newText = string.IsNullOrEmpty(newCompanyCode) ? "System View (All)" : newCompanyCode;

            var log = new UserActivityLog
            {
                UserId = userId,
                Username = username,
                Role = "Super Admin",
                ActivityType = "CompanyContextChanged",
                PreviousCompanyId = prevCompanyId,
                PreviousCompanyCode = prevCompanyCode,
                NewCompanyId = newCompanyId,
                NewCompanyCode = newCompanyCode,
                Description = $"Super Admin switched active working context from '{prevText}' to '{newText}'.",
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording company context switch for user {Username}.", username);
        }
    }

    public async Task<(List<LoginHistory> Items, int TotalCount)> GetLoginHistoryAsync(LoginHistoryFilterViewModel filter, ClaimsPrincipal currentUser)
    {
        var isSuperAdmin = currentUser.IsInRole("Super Admin");
        var isAdmin = currentUser.IsInRole("Admin") || currentUser.IsInRole("CompanyAdmin");
        var userCompanyClaim = currentUser.FindFirst("CompanyId")?.Value;
        var currentUserId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.LoginHistories
            .IgnoreQueryFilters()
            .Include(h => h.Company)
            .AsNoTracking();

        // Strict Server-Side Security Enforcement
        if (!isSuperAdmin)
        {
            if (int.TryParse(userCompanyClaim, out var tenantCompanyId))
            {
                query = query.Where(h => h.CompanyId == tenantCompanyId);
            }
            else
            {
                // Unassigned non-superadmin sees nothing
                return (new List<LoginHistory>(), 0);
            }

            if (!isAdmin)
            {
                // Regular company user only sees their own login activity
                query = query.Where(h => h.UserId == currentUserId);
            }
        }
        else
        {
            // Super Admin can filter by company
            if (filter.CompanyId.HasValue)
            {
                if (filter.CompanyId.Value > 0)
                {
                    query = query.Where(h => h.CompanyId == filter.CompanyId.Value);
                }
                else if (filter.CompanyId.Value == -1)
                {
                    // Filter system-level only (Super Admin logins)
                    query = query.Where(h => h.CompanyId == null);
                }
            }
        }

        // Apply Common Filters
        if (filter.DateFrom.HasValue)
        {
            var fromUtc = filter.DateFrom.Value.Date.ToUniversalTime();
            query = query.Where(h => h.LoginTime >= fromUtc);
        }

        if (filter.DateTo.HasValue)
        {
            var toUtc = filter.DateTo.Value.Date.AddDays(1).ToUniversalTime();
            query = query.Where(h => h.LoginTime < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            var search = filter.Username.Trim();
            query = query.Where(h => h.Username.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "All")
        {
            query = query.Where(h => h.Status == filter.Status);
        }

        var totalCount = await query.CountAsync();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 5 ? 25 : filter.PageSize;

        var items = await query
            .OrderByDescending(h => h.LoginTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<UserActivityLog> Items, int TotalCount)> GetActivityLogsAsync(LoginHistoryFilterViewModel filter, ClaimsPrincipal currentUser)
    {
        var isSuperAdmin = currentUser.IsInRole("Super Admin");
        if (!isSuperAdmin)
        {
            // Only Super Admin accesses cross-system context activity
            return (new List<UserActivityLog>(), 0);
        }

        var query = _context.UserActivityLogs
            .AsNoTracking();

        if (filter.DateFrom.HasValue)
        {
            var fromUtc = filter.DateFrom.Value.Date.ToUniversalTime();
            query = query.Where(l => l.Timestamp >= fromUtc);
        }

        if (filter.DateTo.HasValue)
        {
            var toUtc = filter.DateTo.Value.Date.AddDays(1).ToUniversalTime();
            query = query.Where(l => l.Timestamp < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            var search = filter.Username.Trim();
            query = query.Where(l => l.Username.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 5 ? 25 : filter.PageSize;

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task RecordUserActivityAsync(string? userId, string username, string? role, string activityType, string? description, int? companyId = null, string? ipAddress = null)
    {
        try
        {
            var log = new UserActivityLog
            {
                UserId = userId,
                Username = username,
                Role = role,
                ActivityType = activityType,
                Description = description,
                NewCompanyId = companyId,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording user activity log for {Username}, action {ActivityType}.", username, activityType);
        }
    }
}
