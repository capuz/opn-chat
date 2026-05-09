using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using opn_chat.Application.DTOs;
using opn_chat.Application.Services;
using opn_chat.Domain.Interfaces;

namespace opn_chat.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IMessageRepository _messageRepository;

        public RoomsController(IRoomService roomService, IMessageRepository messageRepository)
        {
            _roomService = roomService;
            _messageRepository = messageRepository;
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
                CreatedByName = r.CreatedBy?.Nickname ?? "Unknown",
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
                CreatedByName = room.CreatedBy?.Nickname ?? "Unknown",
                MemberCount = room.Members?.Count ?? 0
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            if (userId == Guid.Empty) return Unauthorized();

            var room = await _roomService.CreateRoomAsync(userId, dto);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                IsPrivate = room.IsPrivate
            });
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
                Type = m.Type == opn_chat.Domain.Entities.MessageType.Action ? "action" : "normal",
                Timestamp = m.Timestamp,
                ReplyToId = m.ReplyToId?.ToString()
            });
            return Ok(result);
        }
    }
}
