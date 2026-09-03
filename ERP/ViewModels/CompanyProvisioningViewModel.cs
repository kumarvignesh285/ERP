using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ERP.ViewModels;

public class CompanyProvisioningViewModel
{
    // Company Master Details
    [Required(ErrorMessage = "Company Name is required"), MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company Code is required"), MaxLength(20)]
    public string CompanyCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? BusinessType { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; } = "India";

    [MaxLength(20)]
    public string? Pincode { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? AlternatePhone { get; set; }

    [MaxLength(100), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [MaxLength(20)]
    public string? GSTNumber { get; set; }

    [MaxLength(15)]
    public string? PANNumber { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "INR";

    [MaxLength(20)]
    public string? FinancialYear { get; set; }

    public bool IsActive { get; set; } = true;

    public bool CreateSampleData { get; set; } = false;

    public IFormFile? LogoFile { get; set; }

    // Company Administrator Details (Required)
    [Required(ErrorMessage = "Company Admin Name is required"), MaxLength(100)]
    public string AdminFullName { get; set; } = string.Empty;

    public string? AdminUsername { get; set; }

    [Required(ErrorMessage = "Admin Password is required"), DataType(DataType.Password)]
    public string AdminPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the admin password"), DataType(DataType.Password), Compare(nameof(AdminPassword), ErrorMessage = "Admin passwords do not match.")]
    public string AdminConfirmPassword { get; set; } = string.Empty;

    [EmailAddress]
    public string? AdminEmail { get; set; }

    public string? AdminMobile { get; set; }

    // Initial Company User Details (Optional)
    public bool CreateInitialUser { get; set; } = false;

    [MaxLength(100)]
    public string? UserFullName { get; set; }

    public string? UserUsername { get; set; }

    [DataType(DataType.Password)]
    public string? UserPassword { get; set; }

    [DataType(DataType.Password)]
    public string? UserConfirmPassword { get; set; }

    [EmailAddress]
    public string? UserEmail { get; set; }

    public string? UserMobile { get; set; }

    public string? UserRole { get; set; } = "CompanyUser";
}
