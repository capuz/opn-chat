using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using opn_chat.Application.Interfaces;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;

namespace opn_chat.WebAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IPresenceTracker _presenceTracker;
        private readonly IRoomMemberRepository _roomMemberRepo;
        private readonly IRoomRepository _roomRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAdminService _adminService;

        public ChatHub(
            IChatService chatService,
            IPresenceTracker presenceTracker,
            IRoomMemberRepository roomMemberRepo,
            IRoomRepository roomRepository,
            IUnitOfWork unitOfWork,
            IAdminService adminService)
        {
            _chatService = chatService;
            _presenceTracker = presenceTracker;
            _roomMemberRepo = roomMemberRepo;
            _roomRepository = roomRepository;
            _unitOfWork = unitOfWork;
            _adminService = adminService;
        }

        public async Task SendMessage(string roomId, string content, string? replyToId = null, string? messageType = null)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            var member = await _roomMemberRepo.GetUserRoomMemberAsync(Guid.Parse(userId), Guid.Parse(roomId));
            if (member?.IsMuted == true)
            {
                await Clients.Caller.SendAsync("MutedError", "You are muted in this room.");
                return;
            }

            var type = messageType?.ToLowerInvariant() == "action"
                ? MessageType.Action
                : MessageType.Normal;

            var message = await _chatService.SaveMessageAsync(
                Guid.Parse(roomId),
                Guid.Parse(userId),
                content,
                replyToId != null ? Guid.Parse(replyToId) : null,
                type
            );

            var room = await _roomRepository.GetByIdAsync(Guid.Parse(roomId));
            if (room != null)
            {
                room.LastActivityAt = DateTime.UtcNow;
                await _roomRepository.UpdateAsync(room);
                await _unitOfWork.CommitAsync();
            }

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new
            {
                Id = message.Id,
                UserId = userId,
                UserName = userName,
                Content = content,
                Type = type == MessageType.Action ? "action" : "normal",
                Timestamp = message.Timestamp,
                ReplyToId = replyToId,
                Badge = _presenceTracker.GetBadge(userId)
            });
        }

        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await Clients.Group(roomId).SendAsync("UserJoined", userId);
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await Clients.Group(roomId).SendAsync("UserLeft", userId);
        }

        public async Task TypingIndicator(string roomId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await Clients.Group(roomId).SendAsync("UserTyping", userId);
        }

        public async Task KickUser(string roomId, string targetUserId)
        {
            var callerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var callerName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(callerId)) return;

            var callerMember = await _roomMemberRepo.GetUserRoomMemberAsync(Guid.Parse(callerId), Guid.Parse(roomId));
            var callerUser = _presenceTracker.GetOnlineUsers().FirstOrDefault(u => u.Id == callerId);
            bool callerIsAdmin = callerUser != null && false; // presence doesn't expose IsAdmin; rely on role only
            var callerRoleId = callerMember?.RoleId ?? RoleIds.Member;

            if (!await _adminService.CanExecuteAsync("kick", callerRoleId, callerIsAdmin))
            {
                await Clients.Caller.SendAsync("PermissionDenied", "You don't have permission to kick users.");
                return;
            }

            var targetMember = await _roomMemberRepo.GetUserRoomMemberAsync(Guid.Parse(targetUserId), Guid.Parse(roomId));
            if (targetMember != null)
            {
                await _roomMemberRepo.DeleteAsync(targetMember);
                await _unitOfWork.CommitAsync();
            }

            await Clients.User(targetUserId).SendAsync("KickedFromRoom", new { roomId, by = callerName });
            await Clients.Group(roomId).SendAsync("UserKicked", new { userId = targetUserId, by = callerName });
        }

        public async Task MuteUser(string roomId, string targetUserId, bool mute)
        {
            var callerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var callerName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(callerId)) return;

            var callerMember = await _roomMemberRepo.GetUserRoomMemberAsync(Guid.Parse(callerId), Guid.Parse(roomId));
            var callerRoleId = callerMember?.RoleId ?? RoleIds.Member;

            if (!await _adminService.CanExecuteAsync("mute", callerRoleId, false))
            {
                await Clients.Caller.SendAsync("PermissionDenied", "You don't have permission to mute users.");
                return;
            }

            var targetMember = await _roomMemberRepo.GetUserRoomMemberAsync(Guid.Parse(targetUserId), Guid.Parse(roomId));
            if (targetMember != null)
            {
                targetMember.IsMuted = mute;
                await _roomMemberRepo.UpdateAsync(targetMember);
                await _unitOfWork.CommitAsync();
            }

            var evt = mute ? "UserMuted" : "UserUnmuted";
            await Clients.Group(roomId).SendAsync(evt, new { userId = targetUserId, by = callerName });
        }

        public async Task SetTopic(string roomId, string topic)
        {
            var callerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var callerName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(callerId)) return;

            var callerMember = await _roomMemberRepo.GetUserRoomMemberAsync(Guid.Parse(callerId), Guid.Parse(roomId));
            var callerRoleId = callerMember?.RoleId ?? RoleIds.Member;

            if (!await _adminService.CanExecuteAsync("topic", callerRoleId, false))
            {
                await Clients.Caller.SendAsync("PermissionDenied", "You don't have permission to set the topic.");
                return;
            }

            var room = await _roomRepository.GetByIdAsync(Guid.Parse(roomId));
            if (room != null)
            {
                room.Description = topic;
                await _roomRepository.UpdateAsync(room);
                await _unitOfWork.CommitAsync();
            }

            await Clients.Group(roomId).SendAsync("TopicChanged", new { roomId, topic, by = callerName });
        }

        public override Task OnConnectedAsync() => base.OnConnectedAsync();
        public override Task OnDisconnectedAsync(Exception? exception) => base.OnDisconnectedAsync(exception);
    }
}
