using ChaoticGrid.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Configurations;

public sealed class TileConfiguration : IEntityTypeConfiguration<Tile>
{
    public void Configure(EntityTypeBuilder<Tile> builder)
    {
        builder.ToTable("Tiles");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Text)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.IsApproved)
            .IsRequired();

        builder.Property<Guid>("BoardId");
        builder.HasIndex("BoardId");
    }
}
