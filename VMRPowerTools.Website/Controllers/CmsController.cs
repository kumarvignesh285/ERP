using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VMRPowerTools.Domain.Entities;
using VMRPowerTools.Infrastructure.Data;

namespace VMRPowerTools.Website.Controllers;

[Authorize]
public class CmsController : Controller
{
    private readonly WebsiteDbContext _context;
    private readonly ILogger<CmsController> _logger;

    public CmsController(WebsiteDbContext context, ILogger<CmsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var settings = await _context.CmsSettings.ToListAsync();
            
            // Ensure default values are populated if table is fresh/empty
            if (!settings.Any())
            {
                await SeedDefaultCmsSettingsAsync();
                settings = await _context.CmsSettings.ToListAsync();
            }

            ViewBag.SubscribersCount = await _context.NewsletterSubscriptions.CountAsync();
            ViewBag.BlogsCount = await _context.CmsBlogPosts.CountAsync();
            ViewBag.LeadsCount = await _context.Leads.CountAsync();

            return View(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading CMS options dashboard.");
            return View(new List<CmsSetting>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSetting(string key, string value)
    {
        try
        {
            var setting = await _context.CmsSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting != null)
            {
                setting.SettingValue = value;
                _context.CmsSettings.Update(setting);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Config '{key}' updated successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CMS option setting key {Key}.", key);
            TempData["ErrorMessage"] = "Failed to update configuration.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> BlogList()
    {
        var posts = await _context.CmsBlogPosts.OrderByDescending(p => p.PublishedDate).ToListAsync();
        return View(posts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBlog(int id, string title, string category, string summary, string content, string? imagePath)
    {
        try
        {
            var slug = title.Trim().ToLower().Replace(" ", "-").Replace("&", "and").Replace("?", "");
            if (id == 0)
            {
                var newPost = new CmsBlogPost
                {
                    Title = title,
                    Slug = slug,
                    Category = category,
                    Summary = summary,
                    Content = content,
                    ImagePath = imagePath ?? "https://images.unsplash.com/photo-1504917595217-d4dc5ebe6122?auto=format&fit=crop&q=80&w=400",
                    PublishedDate = DateTime.Today,
                    Author = User.Identity?.Name ?? "VMR Admin",
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.Identity?.Name ?? "Admin"
                };
                await _context.CmsBlogPosts.AddAsync(newPost);
                TempData["SuccessMessage"] = "Blog article created.";
            }
            else
            {
                var post = await _context.CmsBlogPosts.FindAsync(id);
                if (post != null)
                {
                    post.Title = title;
                    post.Slug = slug;
                    post.Category = category;
                    post.Summary = summary;
                    post.Content = content;
                    if (!string.IsNullOrEmpty(imagePath)) post.ImagePath = imagePath;
                    _context.CmsBlogPosts.Update(post);
                    TempData["SuccessMessage"] = "Blog article updated.";
                }
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving blog article.");
            TempData["ErrorMessage"] = "Failed to save article.";
        }
        return RedirectToAction(nameof(BlogList));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBlog(int id)
    {
        try
        {
            var post = await _context.CmsBlogPosts.FindAsync(id);
            if (post != null)
            {
                _context.CmsBlogPosts.Remove(post);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blog post.");
        }
        return Json(new { success = false });
    }

    [HttpGet]
    public async Task<IActionResult> Subscribers()
    {
        var subs = await _context.NewsletterSubscriptions.OrderByDescending(s => s.SubscribedAt).ToListAsync();
        return View(subs);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Please enter an email address." });
            }

            var cleanEmail = email.Trim().ToLower();
            var exists = await _context.NewsletterSubscriptions.AnyAsync(s => s.Email == cleanEmail);
            if (!exists)
            {
                var sub = new NewsletterSubscription
                {
                    Email = cleanEmail,
                    SubscribedAt = DateTime.Now,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Public Website"
                };
                await _context.NewsletterSubscriptions.AddAsync(sub);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Successfully subscribed to our corporate catalog newsletter." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording newsletter subscription.");
            return Json(new { success = false, message = "An error occurred while saving your subscription." });
        }
    }

    private async Task SeedDefaultCmsSettingsAsync()
    {
        var defaults = new List<CmsSetting>
        {
            new() { SettingKey = "Contact_Phone", SettingValue = "+91 98765 43210", Description = "Public contact phone number" },
            new() { SettingKey = "Contact_Email", SettingValue = "sales@vmrpowertools.com", Description = "Public contact email address" },
            new() { SettingKey = "Contact_WhatsApp", SettingValue = "919876543210", Description = "WhatsApp API numeric prefix" },
            new() { SettingKey = "SEO_Title_Fallback", SettingValue = "Premium Power Tools & Machinery spares", Description = "Global sitemap title suffix" },
            new() { SettingKey = "Social_Facebook", SettingValue = "https://facebook.com/vmrpowertools", Description = "Facebook company link" },
            new() { SettingKey = "Social_Twitter", SettingValue = "https://twitter.com/vmrpowertools", Description = "Twitter corporate link" },
            new() { SettingKey = "Homepage_Banner_Offer_Text", SettingValue = "Wholesale calibrations up to 15% discount!", Description = "Promotion bar text" }
        };

        foreach (var s in defaults)
        {
            s.CreatedAt = DateTime.Now;
            s.CreatedBy = "System Seed";
        }

        await _context.CmsSettings.AddRangeAsync(defaults);
        await _context.SaveChangesAsync();
    }
}
