using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.GoogleId).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Nickname).IsUnique();
            
            builder.Property(u => u.GoogleId).IsRequired();
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
            builder.Property(u => u.Nickname).IsRequired().HasMaxLength(50);
            builder.Property(u => u.AvatarUrl).HasMaxLength(512);
            builder.Property(u => u.Bio).HasMaxLength(500);
            builder.Property(u => u.Status).HasMaxLength(100);
            builder.Property(u => u.CountryCode).HasMaxLength(2);
            builder.Property(u => u.GlobalBadge).HasMaxLength(20);
            
            builder.HasMany(u => u.RoomMembers)
                .WithOne(rm => rm.User)
                .HasForeignKey(rm => rm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasMany(u => u.Messages)
                .WithOne(m => m.User)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany(u => u.SentMessages)
                .WithOne(pm => pm.Sender)
                .HasForeignKey(pm => pm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasMany(u => u.ReceivedMessages)
                .WithOne(pm => pm.Receiver)
                .HasForeignKey(pm => pm.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
