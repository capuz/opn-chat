using System.Text.RegularExpressions;
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
        private readonly ISystemSettingRepository _settings;

        private static readonly Regex RoomNameRegex = new(@"^#[a-z0-9\-_]{3,30}$", RegexOptions.Compiled);
        private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
            { "#admin", "#system", "#support" };

        public RoomService(
            IRoomRepository roomRepository,
            IRoomMemberRepository roomMemberRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ISystemSettingRepository settings)
        {
            _roomRepository = roomRepository;
            _roomMemberRepository = roomMemberRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _settings = settings;
        }

        public async Task<CreateRoomResultDto> CreateRoomAsync(Guid userId, CreateRoomDto dto)
        {
            var allowCreation = await _settings.GetValueAsync("AllowRoomCreation");
            if (allowCreation?.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
                return new CreateRoomResultDto { Error = RoomCreationError.RoomCreationDisabled };

            if (!RoomNameRegex.IsMatch(dto.Name) || ReservedNames.Contains(dto.Name))
                return new CreateRoomResultDto { Error = RoomCreationError.InvalidName };

            var existing = await _roomRepository.GetByNameAsync(dto.Name);
            if (existing != null)
                return new CreateRoomResultDto { Error = RoomCreationError.NameTaken };

            var todayCount = await _roomRepository.CountCreatedTodayByUserAsync(userId);
            if (todayCount >= 3)
                return new CreateRoomResultDto { Error = RoomCreationError.DailyLimitReached };

            var activeCount = await _roomRepository.CountActiveByUserAsync(userId);
            if (activeCount >= 10)
                return new CreateRoomResultDto { Error = RoomCreationError.ActiveLimitReached };

            var room = new Room
            {
                Name = dto.Name,
                Description = dto.Description,
                IsPrivate = dto.IsPrivate,
                PasswordHash = dto.IsPrivate ? dto.Password : null,
                CreatedById = userId,
                LastActivityAt = DateTime.UtcNow
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

            return new CreateRoomResultDto
            {
                Room = new RoomDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    Description = room.Description,
                    IsPrivate = room.IsPrivate,
                    IsSystem = room.IsSystem,
                    IsArchived = room.IsArchived,
                    MemberCount = 1
                }
            };
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
                if (string.IsNullOrEmpty(password) || password != room.PasswordHash)
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
