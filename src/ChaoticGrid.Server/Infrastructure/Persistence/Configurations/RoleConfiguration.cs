using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public static readonly Guid ObserverRoleId = Guid.Parse("2d4b2b17-0f6d-4b5d-98c5-4020e1c8b0c6");
    public static readonly Guid PlayerRoleId = Guid.Parse("bdf7396f-9f8f-4c47-88f0-4b5bfb9b8c30");
    public static readonly Guid ModeratorRoleId = Guid.Parse("1f9c3666-0a8c-46c5-821d-2f261ccf9e89");
    public static readonly Guid AdminRoleId = Guid.Parse("36f03c86-51be-4a9e-b007-b1d592cb6d2e");

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Permissions)
            .HasConversion<long>();

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasData(
            new Role(ObserverRoleId, "Observer", SystemPermission.Login),
            new Role(PlayerRoleId, "Board Creator", SystemPermission.Login | SystemPermission.CreateBoard),
            new Role(ModeratorRoleId, "System Moderator", SystemPermission.Login | SystemPermission.CreateBoard | SystemPermission.ManageSystemUsers),
            new Role(AdminRoleId, "Admin", SystemPermission.All)
        );
    }
}
