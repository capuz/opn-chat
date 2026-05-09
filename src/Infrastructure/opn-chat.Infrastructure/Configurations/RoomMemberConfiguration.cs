using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class RoomMemberConfiguration : IEntityTypeConfiguration<RoomMember>
    {
        public void Configure(EntityTypeBuilder<RoomMember> builder)
        {
            builder.HasKey(rm => rm.Id);
            
            builder.HasOne(rm => rm.User)
                .WithMany(u => u.RoomMembers)
                .HasForeignKey(rm => rm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(rm => rm.Room)
                .WithMany(r => r.Members)
                .HasForeignKey(rm => rm.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(rm => rm.Role)
                .WithMany(r => r.RoomMembers)
                .HasForeignKey(rm => rm.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasIndex(rm => new { rm.UserId, rm.RoomId }).IsUnique();
        }
    }
}
