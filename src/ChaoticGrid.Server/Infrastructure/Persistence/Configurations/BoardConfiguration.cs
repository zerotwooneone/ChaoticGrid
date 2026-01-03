using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Configurations;

public sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("Boards");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasConversion(
                id => id.Value,
                value => new BoardId(value))
            .ValueGeneratedNever();

        builder.Property(b => b.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.MinimumApprovedTilesToStart)
            .IsRequired();

        builder.HasMany(b => b.Tiles)
            .WithOne()
            .HasForeignKey("BoardId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Players)
            .WithOne()
            .HasForeignKey("BoardId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Tiles).AutoInclude();
        builder.Navigation(b => b.Players).AutoInclude();
    }
}
