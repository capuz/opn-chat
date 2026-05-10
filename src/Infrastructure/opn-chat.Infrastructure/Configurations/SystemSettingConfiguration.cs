using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
    {
        public void Configure(EntityTypeBuilder<SystemSetting> builder)
        {
            builder.HasKey(s => s.Key);
            builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Value).HasMaxLength(1000);
        }
    }
}
