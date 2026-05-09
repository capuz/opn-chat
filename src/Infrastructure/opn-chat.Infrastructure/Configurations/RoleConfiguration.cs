using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using opn_chat.Domain.Entities;

namespace opn_chat.Infrastructure.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);
            builder.HasIndex(r => r.Name).IsUnique();
            
            builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
            builder.Property(r => r.Description).HasMaxLength(200);
            
            // Seed default roles
            builder.HasData(
                new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "owner", Description = "Room owner with full control" },
                new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "moderator", Description = "Room moderator with kick/ban powers" },
                new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "member", Description = "Regular room member" }
            );
        }
    }
}
