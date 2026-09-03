using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models;

public class Lead : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

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
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}

public class FollowUp : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int LeadId { get; set; }
    public Lead? Lead { get; set; }
    public DateTime FollowUpDate { get; set; } = DateTime.Now;
    [MaxLength(50)]
    public string FollowUpType { get; set; } = "Call"; // Call, Email, Meeting, Chat
    [MaxLength(1000)]
    public string Remarks { get; set; } = string.Empty;
    public DateTime? NextFollowUpDate { get; set; }
    [MaxLength(50)]
    public string Status { get; set; } = "Completed"; // Pending, Completed
}

public class Opportunity : BaseEntity, ICompanyOwned
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int LeadId { get; set; }
    public Lead? Lead { get; set; }
    [Required, MaxLength(200)]
    public string OpportunityName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedRevenue { get; set; }
    public DateTime? CloseDate { get; set; }
    [MaxLength(50)]
    public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost
    [Range(0, 100)]
    public double Probability { get; set; }
    [MaxLength(1000)]
    public string? Remarks { get; set; }
}
