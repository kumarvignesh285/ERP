using Microsoft.AspNetCore.Identity;

namespace ERP.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ClearTextPassword { get; set; }
    public string? Mobile { get; set; }
    public string? ProfilePhoto { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
}
