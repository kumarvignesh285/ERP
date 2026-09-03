using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ERP.Models;
using ERP.ViewModels;

namespace ERP.Interfaces;

public interface ILoginHistoryService
{
    Task RecordSuccessfulLoginAsync(AppUser user, string sessionId, string? ipAddress, string? userAgent);
    Task RecordFailedLoginAsync(string username, string reason, string? ipAddress, string? userAgent, int? companyId = null, string? companyCode = null);
    Task RecordLogoutAsync(string? userId, string? sessionId);
    Task RecordCompanySwitchAsync(string userId, string username, int? prevCompanyId, string? prevCompanyCode, int? newCompanyId, string? newCompanyCode, string? ipAddress);
    Task<(List<LoginHistory> Items, int TotalCount)> GetLoginHistoryAsync(LoginHistoryFilterViewModel filter, ClaimsPrincipal currentUser);
    Task<(List<UserActivityLog> Items, int TotalCount)> GetActivityLogsAsync(LoginHistoryFilterViewModel filter, ClaimsPrincipal currentUser);
    Task RecordUserActivityAsync(string? userId, string username, string? role, string activityType, string? description, int? companyId = null, string? ipAddress = null);
}
