using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using opn_chat.Application.DTOs;
using opn_chat.Application.Services;
using opn_chat.WebAPI.Hubs;

namespace opn_chat.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrivateChatController : ControllerBase
    {
        private readonly IPrivateMessageService _privateMessageService;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public PrivateChatController(IPrivateMessageService privateMessageService, IHubContext<NotificationHub> notificationHub)
        {
            _privateMessageService = privateMessageService;
            _notificationHub = notificationHub;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendPrivateMessageDto dto)
        {
            var senderId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (senderId == Guid.Empty) return Unauthorized();

            var message = await _privateMessageService.SendMessageAsync(senderId, dto);
            var senderNick = User.FindFirst(ClaimTypes.Name)?.Value ?? "User";

            await _notificationHub.Clients
                .User(dto.ReceiverId.ToString())
                .SendAsync("NewDirectMessage", new { senderId = senderId.ToString(), senderNick });

            return Ok(new PrivateMessageDto
            {
                Id = message.Id,
                SenderId = senderId.ToString(),
                SenderName = senderNick,
                ReceiverName = message.Receiver?.Nickname ?? "Unknown",
                Content = message.Content,
                Timestamp = message.Timestamp,
                IsRead = message.IsRead,
                IsDeletedForEveryone = false
            });
        }

        [HttpGet("conversation/{userId}")]
        public async Task<IActionResult> GetConversation(Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (currentUserId == Guid.Empty) return Unauthorized();

            var messages = await _privateMessageService.GetConversationAsync(currentUserId, userId, currentUserId, skip, take);
            var result = messages.Select(m => new PrivateMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId.ToString(),
                SenderName = m.Sender?.Nickname ?? "Unknown",
                ReceiverName = m.Receiver?.Nickname ?? "Unknown",
                Content = m.IsDeletedForEveryone ? string.Empty : m.Content,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead,
                IsDeletedForEveryone = m.IsDeletedForEveryone
            });
            return Ok(result);
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetRecentConversations()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var conversations = await _privateMessageService.GetRecentConversationsAsync(userId);
            return Ok(conversations);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var count = await _privateMessageService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpPost("mark-read/{messageId}")]
        public async Task<IActionResult> MarkAsRead(Guid messageId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _privateMessageService.MarkAsReadAsync(messageId, userId);
            return result ? Ok() : BadRequest();
        }

        [HttpPost("mark-conversation-read/{partnerId}")]
        public async Task<IActionResult> MarkConversationAsRead(Guid partnerId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            await _privateMessageService.MarkConversationAsReadAsync(partnerId, userId);
            return Ok();
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId, [FromQuery] bool forEveryone = false)
        {
            var requesterId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (requesterId == Guid.Empty) return Unauthorized();

            var (success, error, message) = await _privateMessageService.DeleteMessageAsync(messageId, requesterId, forEveryone);
            if (!success) return BadRequest(new { error });

            if (forEveryone && message != null)
            {
                var otherId = message.SenderId == requesterId
                    ? message.ReceiverId.ToString()
                    : message.SenderId.ToString();

                await _notificationHub.Clients
                    .User(otherId)
                    .SendAsync("PrivateMessageDeleted", new { messageId = messageId.ToString() });
            }

            return Ok();
        }
    }
}
