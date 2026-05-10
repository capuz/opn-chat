using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using opn_chat.Application.DTOs;
using opn_chat.Application.Services;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;
using opn_chat.WebAPI.Hubs;

namespace opn_chat.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IMessageRepository _messageRepository;
        private readonly IHubContext<ChatHub> _chatHub;

        public RoomsController(
            IRoomService roomService,
            IMessageRepository messageRepository,
            IHubContext<ChatHub> chatHub)
        {
            _roomService = roomService;
            _messageRepository = messageRepository;
            _chatHub = chatHub;
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicRooms()
        {
            var rooms = await _roomService.GetPublicRoomsAsync();
            var result = rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsPrivate = r.IsPrivate,
                IsSystem = r.IsSystem,
                IsArchived = r.IsArchived,
                CreatedByName = r.CreatedBy?.Nickname ?? "System",
                MemberCount = r.Members?.Count ?? 0
            });
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoom(Guid id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null) return NotFound();

            return Ok(new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                IsPrivate = room.IsPrivate,
                IsSystem = room.IsSystem,
                IsArchived = room.IsArchived,
                CreatedByName = room.CreatedBy?.Nickname ?? "System",
                MemberCount = room.Members?.Count ?? 0
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _roomService.CreateRoomAsync(userId, dto);

            if (!result.Success)
            {
                return result.Error switch
                {
                    RoomCreationError.InvalidName => BadRequest(new { code = "INVALID_NAME" }),
                    RoomCreationError.NameTaken => Conflict(new { code = "NAME_TAKEN" }),
                    RoomCreationError.DailyLimitReached => StatusCode(429, new { code = "DAILY_LIMIT" }),
                    RoomCreationError.ActiveLimitReached => StatusCode(429, new { code = "ACTIVE_LIMIT" }),
                    RoomCreationError.RoomCreationDisabled => StatusCode(403, new { code = "CREATION_DISABLED" }),
                    _ => BadRequest(new { code = "UNKNOWN_ERROR" })
                };
            }

            var room = result.Room!;
            var nickname = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var systemContent = $"{nickname} created room {room.Name}";
            var roomId = room.Id.ToString();

            await _chatHub.Clients.Group(roomId).SendAsync("ReceiveMessage", new
            {
                Id = Guid.NewGuid().ToString(),
                UserId = (string?)null,
                UserName = "System",
                Content = systemContent,
                Type = "system",
                Timestamp = DateTime.UtcNow,
                ReplyToId = (string?)null
            });

            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }

        [HttpPost("{roomId}/join")]
        public async Task<IActionResult> JoinRoom(Guid roomId, [FromBody] string? password = null)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _roomService.JoinRoomAsync(userId, roomId, password);
            return result ? Ok() : BadRequest(new { message = "Could not join room" });
        }

        [HttpDelete("{roomId}/leave")]
        public async Task<IActionResult> LeaveRoom(Guid roomId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _roomService.LeaveRoomAsync(userId, roomId);
            return result ? Ok() : BadRequest(new { message = "Could not leave room" });
        }

        [HttpGet("{roomId}/members")]
        public async Task<IActionResult> GetRoomMembers(Guid roomId)
        {
            var members = await _roomService.GetRoomMembersAsync(roomId);
            var result = members.Select(m => new RoomMemberDto
            {
                UserId = m.UserId,
                Nickname = m.User?.Nickname ?? "Unknown",
                AvatarUrl = m.User?.AvatarUrl,
                RoleName = m.Role?.Name ?? "member",
                JoinedAt = m.JoinedAt
            });
            return Ok(result);
        }

        [HttpGet("{roomId}/messages")]
        public async Task<IActionResult> GetRoomMessages(Guid roomId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var messages = await _messageRepository.GetRoomMessagesAsync(roomId, skip, take);
            var result = messages.Select(m => new
            {
                Id = m.Id,
                UserId = m.UserId.ToString(),
                UserName = m.User?.Nickname ?? m.User?.Email ?? "Unknown",
                Content = m.Content,
                Type = m.Type == MessageType.Action ? "action" : m.Type == MessageType.System ? "system" : "normal",
                Timestamp = m.Timestamp,
                ReplyToId = m.ReplyToId?.ToString()
            });
            return Ok(result);
        }
    }
}
