using ERP.Models;

namespace ERP.Interfaces;

public interface INotificationService
{
    Task<List<Notification>> GetNotificationsAsync();
    Task AddNotificationAsync(string type, string message, string? linkUrl = null);
    Task MarkAsReadAsync(int id);
    Task MarkAllAsReadAsync();
}
