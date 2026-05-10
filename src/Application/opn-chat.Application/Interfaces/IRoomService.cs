using opn_chat.Application.DTOs;
using opn_chat.Domain.Entities;

namespace opn_chat.Application.Services
{
    public interface IRoomService
    {
        Task<CreateRoomResultDto> CreateRoomAsync(Guid userId, CreateRoomDto dto);
        Task<IEnumerable<Room>> GetPublicRoomsAsync();
        Task<Room?> GetRoomByIdAsync(Guid roomId);
        Task<bool> JoinRoomAsync(Guid userId, Guid roomId, string? password = null);
        Task<bool> LeaveRoomAsync(Guid userId, Guid roomId);
        Task<IEnumerable<RoomMember>> GetRoomMembersAsync(Guid roomId);
        Task<bool> IsUserMemberAsync(Guid userId, Guid roomId);
        Task<bool> HasPermissionAsync(Guid userId, Guid roomId, string permission);
    }
}
