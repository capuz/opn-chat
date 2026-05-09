using System;
using System.Collections.Generic;

namespace opn_chat.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty; // owner, moderator, member
        public string? Description { get; set; }
        
        // Navigation properties
        public ICollection<RoomMember> RoomMembers { get; set; } = new List<RoomMember>();
    }
}
