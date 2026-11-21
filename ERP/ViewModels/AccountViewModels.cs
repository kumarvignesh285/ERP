using System.ComponentModel.DataAnnotations;

namespace ERP.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Username or Email is required.")]
    [Display(Name = "Username / Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me on this device")]
    public bool RememberMe { get; set; }

    public string ReturnUrl { get; set; } = "~/";
}

public sealed class UserProfileViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
    public ChangePasswordViewModel ChangePassword { get; set; } = new();
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
