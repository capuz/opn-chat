using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.HasKey(r => r.Id);
            
            builder.Property(r => r.Reason).IsRequired().HasMaxLength(100);
            builder.Property(r => r.Details).HasMaxLength(1000);
            
            builder.HasOne(r => r.ReportedBy)
                .WithMany(u => u.ReportsFiled)
                .HasForeignKey(r => r.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(r => r.Message)
                .WithMany(m => m.Reports)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
