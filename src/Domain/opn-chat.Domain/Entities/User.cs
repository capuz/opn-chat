using System;
using System.Collections.Generic;

namespace opn_chat.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string GoogleId { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Status { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int NicknameChangesToday { get; set; } = 0;
        public DateTime? NicknameChangesDate { get; set; } = null;
        public DateTime? NickAdUnlockedUntil { get; set; }
        public string? CountryCode { get; set; }
        public bool ShowFlag { get; set; } = false;
        public string? GlobalBadge { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsDeactivated { get; set; } = false;
        public string PreferredLanguage { get; set; } = "auto";
        public string? Timezone { get; set; }

        // Navigation properties
        public ICollection<RoomMember> RoomMembers { get; set; } = new List<RoomMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<PrivateMessage> SentMessages { get; set; } = new List<PrivateMessage>();
        public ICollection<PrivateMessage> ReceivedMessages { get; set; } = new List<PrivateMessage>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<Report> ReportsFiled { get; set; } = new List<Report>();
        public ICollection<Ban> BansReceived { get; set; } = new List<Ban>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
