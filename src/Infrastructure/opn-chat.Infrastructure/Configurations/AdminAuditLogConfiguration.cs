using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
    {
        public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.AdminNickname).IsRequired().HasMaxLength(50);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
            builder.Property(a => a.TargetType).HasMaxLength(50);
            builder.Property(a => a.TargetId).HasMaxLength(100);
            builder.Property(a => a.TargetDisplay).HasMaxLength(200);
        }
    }
}
