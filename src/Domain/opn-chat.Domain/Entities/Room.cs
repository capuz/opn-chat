using System;
using System.Collections.Generic;

namespace opn_chat.Domain.Entities
{
    public class Room
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; } = false;
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public bool IsLocked { get; set; } = false;
        public bool IsSystem { get; set; } = false;
        public bool IsArchived { get; set; } = false;
        public DateTime? LastActivityAt { get; set; }

        // Navigation properties
        public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
