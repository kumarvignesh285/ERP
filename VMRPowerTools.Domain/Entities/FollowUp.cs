using System;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class FollowUp : BaseEntity
{
    public int LeadId { get; set; }
    public Lead? Lead { get; set; }

    public DateTime FollowUpDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string FollowUpType { get; set; } = "Call"; // Call, Email, Meeting, Chat

    [Required, MaxLength(1000)]
    public string Remarks { get; set; } = string.Empty;

    public DateTime? NextFollowUpDate { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Completed"; // Pending, Completed
}
