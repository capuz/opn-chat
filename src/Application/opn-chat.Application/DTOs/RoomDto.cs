using opn_chat.Domain.Entities;

namespace opn_chat.Application.DTOs
{
    public class RoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public string? CreatedByName { get; set; }
        public int MemberCount { get; set; }
    }

    public class CreateRoomDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public string? Password { get; set; }
    }

    public class RoomMemberDto
    {
        public Guid UserId { get; set; }
        public string Nickname { get; set; }
        public string? AvatarUrl { get; set; }
        public string RoleName { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
