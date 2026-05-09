using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class BanConfiguration : IEntityTypeConfiguration<Ban>
    {
        public void Configure(EntityTypeBuilder<Ban> builder)
        {
            builder.HasKey(b => b.Id);
            
            builder.Property(b => b.Reason).IsRequired().HasMaxLength(200);
            
            builder.HasOne(b => b.User)
                .WithMany(u => u.BansReceived)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(b => b.BannedBy)
                .WithMany()
                .HasForeignKey(b => b.BannedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasIndex(b => new { b.UserId, b.IsActive });
        }
    }
}
