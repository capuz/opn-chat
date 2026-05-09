using opn_chat.Application.DTOs;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;

namespace opn_chat.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomMemberRepository _roomMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RoomService(
            IRoomRepository roomRepository,
            IRoomMemberRepository roomMemberRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _roomRepository = roomRepository;
            _roomMemberRepository = roomMemberRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Room?> CreateRoomAsync(Guid userId, CreateRoomDto dto)
        {
            var room = new Room
            {
                Name = dto.Name,
                Description = dto.Description,
                IsPrivate = dto.IsPrivate,
                PasswordHash = dto.IsPrivate ? dto.Password : null, // TODO: Hash password
                CreatedById = userId
            };

            await _roomRepository.AddAsync(room);

            var roomMember = new RoomMember
            {
                UserId = userId,
                RoomId = room.Id,
                RoleId = RoleIds.Owner
            };

            await _roomMemberRepository.AddAsync(roomMember);
            await _unitOfWork.CommitAsync();

            return room;
        }

        public async Task<IEnumerable<Room>> GetPublicRoomsAsync()
        {
            return await _roomRepository.GetPublicRoomsAsync();
        }

        public async Task<Room?> GetRoomByIdAsync(Guid roomId)
        {
            return await _roomRepository.GetByIdAsync(roomId);
        }

        public async Task<bool> JoinRoomAsync(Guid userId, Guid roomId, string? password = null)
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room == null) return false;

            if (room.IsPrivate)
            {
                if (string.IsNullOrEmpty(password) || password != room.PasswordHash) // TODO: Compare hashed password
                    return false;
            }

            if (await _roomMemberRepository.IsUserMemberAsync(userId, roomId))
                return true;

            var roomMember = new RoomMember
            {
                UserId = userId,
                RoomId = roomId,
                RoleId = RoleIds.Member
            };

            await _roomMemberRepository.AddAsync(roomMember);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<bool> LeaveRoomAsync(Guid userId, Guid roomId)
        {
            var roomMember = await _roomMemberRepository.GetUserRoomMemberAsync(userId, roomId);
            if (roomMember == null) return false;

            await _roomMemberRepository.DeleteAsync(roomMember);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<RoomMember>> GetRoomMembersAsync(Guid roomId)
        {
            return await _roomMemberRepository.GetRoomMembersAsync(roomId);
        }

        public async Task<bool> IsUserMemberAsync(Guid userId, Guid roomId)
        {
            return await _roomRepository.IsUserMemberAsync(userId, roomId);
        }

        public async Task<bool> HasPermissionAsync(Guid userId, Guid roomId, string permission)
        {
            var roomMember = await _roomMemberRepository.GetUserRoomMemberAsync(userId, roomId);
            if (roomMember == null) return false;

            return roomMember.RoleId == RoleIds.Owner || roomMember.RoleId == RoleIds.Moderator;
        }
    }
}
