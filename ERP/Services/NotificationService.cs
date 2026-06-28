using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Interfaces;
using ERP.Models;

namespace ERP.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetNotificationsAsync()
    {
        return await _context.Notifications
            .Where(n => n.IsActive && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task AddNotificationAsync(string type, string message, string? linkUrl = null)
    {
        _context.Notifications.Add(new Notification
        {
            NotificationType = type,
            Message = message,
            LinkUrl = linkUrl,
            IsRead = false
        });
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int id)
    {
        var n = await _context.Notifications.FindAsync(id);
        if (n != null)
        {
            n.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync()
    {
        var list = await _context.Notifications.Where(n => !n.IsRead).ToListAsync();
        foreach (var n in list)
        {
            n.IsRead = true;
        }
        await _context.SaveChangesAsync();
    }
}
