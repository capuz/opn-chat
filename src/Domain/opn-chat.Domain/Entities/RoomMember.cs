using System;

namespace opn_chat.Domain.Entities
{
    public class RoomMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid RoomId { get; set; }
        public Room Room { get; set; } = null!;
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public bool IsMuted { get; set; } = false;
    }
}
