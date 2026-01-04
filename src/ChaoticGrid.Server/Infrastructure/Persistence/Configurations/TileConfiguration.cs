using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Domain.Enums;
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

        builder.Ignore(t => t.IsApproved);

        builder.Property(t => t.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.CreatedByUserId)
            .IsRequired();

        builder.Property(t => t.IsConfirmed)
            .IsRequired();

        builder.Property(t => t.BoardId)
            .HasConversion(
                id => id.Value,
                value => new BoardId(value));
        builder.HasIndex(t => t.BoardId);
    }
}
