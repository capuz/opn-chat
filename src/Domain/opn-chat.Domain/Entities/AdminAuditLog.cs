using System;

namespace opn_chat.Domain.Entities
{
    public class AdminAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AdminId { get; set; }
        public string AdminNickname { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? TargetType { get; set; }
        public string? TargetId { get; set; }
        public string? TargetDisplay { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
