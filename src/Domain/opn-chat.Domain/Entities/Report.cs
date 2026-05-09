using System;

namespace opn_chat.Domain.Entities
{
    public class Report
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReportedById { get; set; }
        public User ReportedBy { get; set; } = null!;
        public Guid? MessageId { get; set; }
        public Message? Message { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;
    }
}
