using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);
            
            builder.Property(n => n.Type).IsRequired().HasMaxLength(50);
            builder.Property(n => n.Content).IsRequired().HasMaxLength(500);
            
            builder.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasIndex(n => new { n.UserId, n.IsRead });
        }
    }
}
