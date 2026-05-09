using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace opn_chat.WebAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string userId, string type, string content)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                Type = type,
                Content = content,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task MarkAsRead(string notificationId)
        {
            // TODO: Update notification in DB
            await Clients.Caller.SendAsync("NotificationMarked", notificationId);
        }
    }
}
