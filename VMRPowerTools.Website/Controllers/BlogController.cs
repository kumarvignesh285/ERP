using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace VMRPowerTools.Website.Controllers;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public string Author { get; set; } = "VMR Technical Desk";
    public string? ImagePath { get; set; }
}

public class BlogController : Controller
{
    private static readonly List<BlogPost> BlogPosts = new()
    {
        new BlogPost
        {
            Id = 1,
            Title = "Safety & Maintenance Guide for Heavy Demolition Hammers",
            Slug = "safety-maintenance-heavy-demolition-hammers",
            Category = "Maintenance",
            Summary = "Learn the essential carbon brush and calibration checklist to double the operational lifecycles of your demolition hammers.",
            Content = @"<p>Heavy-duty demolition hammers undergo massive shock loads during daily operations. Ensuring proper maintenance prevents premature armature failures.</p>
                       <h5>1. Inspect Carbon Brushes Regularly</h5>
                       <p>Worn carbon brushes cause excessive sparking and armature damage. Replace them when they reach the minimum wear line.</p>
                       <h5>2. Gearbox Lubrication</h5>
                       <p>High-friction gearboxes require high-grade grease. Check the gear chamber lubrication levels after every 50 hours of work.</p>
                       <h5>3. Keep Ventilation Slots Clear</h5>
                       <p>Concrete dust clogs the air intakes, causing thermal overload. Blow dry air into ventilation slots after every work shift.</p>",
            PublishedDate = DateTime.Today.AddDays(-10),
            ImagePath = "https://images.unsplash.com/photo-1504917595217-d4dc5ebe6122?auto=format&fit=crop&q=80&w=400"
        },
        new BlogPost
        {
            Id = 2,
            Title = "Understanding GST HSN Code Classifications for Power Spares",
            Slug = "gst-hsn-code-classifications-power-spares",
            Category = "Industry News",
            Summary = "A comprehensive tax outline explaining how HSN code 8467 applies to imported armature spinner assemblies.",
            Content = @"<p>GST compliance requires precise classification of tools and spares under appropriate Harmonized System of Nomenclature (HSN) codes.</p>
                       <h5>1. Base Code HSN 8467</h5>
                       <p>Tools for working in the hand, pneumatic, hydraulic or with self-contained electric or non-electric motor are classified under 8467.</p>
                       <h5>2. Spares Tax Rate</h5>
                       <p>In accordance with tax revisions, original spares (armature coils, field coils) attract 18% GST rate under standard billing schedules.</p>",
            PublishedDate = DateTime.Today.AddDays(-5),
            ImagePath = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&q=80&w=400"
        },
        new BlogPost
        {
            Id = 3,
            Title = "Calibration Best Practices for High-RPM Industrial Sanders",
            Slug = "calibration-practices-high-rpm-sanders",
            Category = "Product Guides",
            Summary = "Discover the exact vibration control calibration required to ensure even surface finishes during sanding operations.",
            Content = @"<p>Sanding discs running at high rotational speeds (above 10,000 RPM) require strict axis calibration to prevent orbital deviations.</p>
                       <h5>1. Backing Pad Balance</h5>
                       <p>Uneven pad wear causes severe vibration. Check backing pad wear profiles before mounting new sanding disks.</p>
                       <h5>2. Bearing Diagnostics</h5>
                       <p>High temperature and noise indicate bearing wear. Replace seal bearings immediately to protect core motor drives.</p>",
            PublishedDate = DateTime.Today.AddDays(-2),
            ImagePath = "https://images.unsplash.com/photo-1504307651254-35680f356dfd?auto=format&fit=crop&q=80&w=400"
        }
    };

    [HttpGet]
    public IActionResult Index(string? category, string? search)
    {
        var posts = BlogPosts.AsEnumerable();

        if (!string.IsNullOrEmpty(category))
        {
            posts = posts.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim().ToLower();
            posts = posts.Where(p => p.Title.ToLower().Contains(s) || p.Summary.ToLower().Contains(s));
        }

        ViewBag.Categories = BlogPosts.Select(p => p.Category).Distinct().ToList();
        ViewBag.SelectedCategory = category;
        ViewBag.Search = search;

        return View(posts.OrderByDescending(p => p.PublishedDate).ToList());
    }

    [HttpGet]
    [Route("blog/{slug}")]
    public IActionResult Details(string slug)
    {
        var post = BlogPosts.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (post == null)
        {
            return NotFound();
        }

        var related = BlogPosts.Where(p => p.Category == post.Category && p.Id != post.Id).Take(2).ToList();
        ViewBag.RelatedPosts = related;

        return View(post);
    }
}
