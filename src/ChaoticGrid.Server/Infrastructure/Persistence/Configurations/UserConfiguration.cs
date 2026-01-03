using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nickname)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasMany(x => x.GlobalRoles)
            .WithMany()
            .UsingEntity(
                "UserGlobalRole",
                r => r.HasOne(typeof(Role)).WithMany().HasForeignKey("RoleId"),
                l => l.HasOne(typeof(User)).WithMany().HasForeignKey("UserId"),
                j =>
                {
                    j.HasKey("UserId", "RoleId");
                }
            );
    }
}
