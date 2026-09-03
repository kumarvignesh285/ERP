using System;
using System.Collections.Generic;
using ERP.Models;

namespace ERP.ViewModels;

public class LoginHistoryFilterViewModel
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Username { get; set; }
    public string? Status { get; set; }
    public int? CompanyId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class LoginHistoryPageViewModel
{
    public List<LoginHistory> Histories { get; set; } = new();
    public List<UserActivityLog> ActivityLogs { get; set; } = new();
    public List<Company> AvailableCompanies { get; set; } = new();

    public LoginHistoryFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Filter.PageSize);
    public bool HasPreviousPage => Filter.Page > 1;
    public bool HasNextPage => Filter.Page < TotalPages;

    public bool IsSuperAdmin { get; set; }
    public string? CurrentUserRole { get; set; }
    public int? UserCompanyId { get; set; }
}
