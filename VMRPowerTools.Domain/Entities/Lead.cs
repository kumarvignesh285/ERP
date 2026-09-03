using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class Lead : BaseEntity
{
    [Required, MaxLength(200)]
    public string LeadName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(200)]
    public string? ContactPerson { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; } // Web, Referral, Cold Call, Social Media

    [MaxLength(50)]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Proposal Sent, Won, Lost

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
