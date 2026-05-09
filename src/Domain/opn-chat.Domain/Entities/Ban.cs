using System;

namespace opn_chat.Domain.Entities
{
    public class Ban
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid BannedById { get; set; }
        public User BannedBy { get; set; } = null!;
        public string Reason { get; set; } = string.Empty;
        public DateTime BannedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
