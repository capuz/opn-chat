using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class PrivateMessageConfiguration : IEntityTypeConfiguration<PrivateMessage>
    {
        public void Configure(EntityTypeBuilder<PrivateMessage> builder)
        {
            builder.HasKey(pm => pm.Id);
            
            builder.Property(pm => pm.Content).IsRequired().HasMaxLength(2000);
            
            builder.HasOne(pm => pm.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(pm => pm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(pm => pm.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(pm => pm.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasIndex(pm => new { pm.SenderId, pm.ReceiverId, pm.Timestamp });
            builder.HasIndex(pm => new { pm.ReceiverId, pm.IsRead });
        }
    }
}
