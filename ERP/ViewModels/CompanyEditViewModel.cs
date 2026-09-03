using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ERP.ViewModels;

public class CompanyEditViewModel
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Company Name is required"), MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    public string? CompanyCode { get; set; }

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

    public IFormFile? LogoFile { get; set; }

    // Admin Details
    public string? AdminUserId { get; set; }

    [MaxLength(100)]
    public string? AdminFullName { get; set; }

    public string? AdminUsername { get; set; }

    [EmailAddress]
    public string? AdminEmail { get; set; }

    public string? AdminMobile { get; set; }

    // Optional Admin Password Reset during edit (leave blank to keep unchanged)
    [DataType(DataType.Password)]
    public string? AdminNewPassword { get; set; }

    [DataType(DataType.Password), Compare(nameof(AdminNewPassword), ErrorMessage = "Passwords do not match.")]
    public string? AdminConfirmPassword { get; set; }
}

public class CompanyUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string Roles { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
