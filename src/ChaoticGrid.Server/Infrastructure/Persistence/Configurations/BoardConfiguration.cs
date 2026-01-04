using System.Text.Json;
using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Configurations;

public sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        builder.Property(b => b.VotesRequiredToConfirm)
            .IsRequired();

        builder.Property(b => b.Votes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Vote>>(v, JsonOptions) ?? new List<Vote>())
            .Metadata.SetValueComparer(new ValueComparer<List<Vote>>(
                (a, b) => a.SequenceEqual(b),
                v => v.Aggregate(0, (acc, next) => HashCode.Combine(acc, next.GetHashCode())),
                v => v.ToList()));

        builder.HasMany(b => b.Tiles)
            .WithOne()
            .HasForeignKey(t => t.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Players)
            .WithOne()
            .HasForeignKey("BoardId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Tiles).AutoInclude();
        builder.Navigation(b => b.Players).AutoInclude();
    }
}
