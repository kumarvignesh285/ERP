using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Employee : BaseEntity
{
    [Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? Designation { get; set; }
    [MaxLength(100)]
    public string? Department { get; set; }
    [MaxLength(20)]
    public string? Mobile { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
    public DateTime? JoinDate { get; set; }
    public decimal? Salary { get; set; }
    [MaxLength(500)]
    public string? Address { get; set; }
    [MaxLength(15)]
    public string? PANNumber { get; set; }
    [MaxLength(20)]
    public string? AadhaarNumber { get; set; }
    [MaxLength(20)]
    public string? BankAccountNumber { get; set; }
    [MaxLength(20)]
    public string? IFSCCode { get; set; }
    [MaxLength(50)]
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
