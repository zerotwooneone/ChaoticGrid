using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
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

        builder.Property(t => t.ProposedByPlayerId);

        builder.Property(t => t.IsConfirmed)
            .IsRequired();

        builder.Property<BoardId>("BoardId")
            .HasConversion(
                id => id.Value,
                value => new BoardId(value));
        builder.HasIndex("BoardId");
    }
}
