using System.ComponentModel.DataAnnotations;

namespace ERP.Models;

public class Notification : BaseEntity
{
    [Required, MaxLength(100)]
    public string NotificationType { get; set; } = "General"; // LowStock, InvoiceDue, PaymentReminder, ApprovalPending, LeadFollowUp
    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    [MaxLength(250)]
    public string? LinkUrl { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
}
