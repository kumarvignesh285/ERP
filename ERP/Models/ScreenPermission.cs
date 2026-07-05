using System;

namespace ERP.Models;

public class ScreenPermission : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string ScreenName { get; set; } = string.Empty; // e.g. "Masters", "Sales", "Purchase", "Inventory", "Accounts", "CRM", "Reports", "Settings"
    public bool CanView { get; set; } = true;
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;
}
