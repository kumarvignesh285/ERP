using System;
using System.ComponentModel.DataAnnotations;

namespace VMRPowerTools.Domain.Entities;

public class NewsletterSubscription : BaseEntity
{
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    public DateTime SubscribedAt { get; set; } = DateTime.Now;
}
