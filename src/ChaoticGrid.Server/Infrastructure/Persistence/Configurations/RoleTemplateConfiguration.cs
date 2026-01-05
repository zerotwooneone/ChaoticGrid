using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Configurations;

public sealed class RoleTemplateConfiguration : IEntityTypeConfiguration<RoleTemplate>
{
    public void Configure(EntityTypeBuilder<RoleTemplate> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerUserId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DefaultBoardPermissions)
            .HasConversion<long>();

        builder.HasIndex(x => new { x.OwnerUserId, x.Name })
            .IsUnique();
    }
}
