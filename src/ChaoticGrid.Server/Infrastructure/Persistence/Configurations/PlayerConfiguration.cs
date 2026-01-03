using System.Text.Json;
using ChaoticGrid.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChaoticGrid.Server.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.DisplayName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(p => p.SilencedUntilUtc);

        builder.Property(p => p.GridTileIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (a, b) => a.SequenceEqual(b),
                v => v.Aggregate(0, (acc, next) => HashCode.Combine(acc, next.GetHashCode())),
                v => v.ToList()));

        builder.Property(p => p.Roles)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => a.SequenceEqual(b),
                v => v.Aggregate(0, (acc, next) => HashCode.Combine(acc, next.GetHashCode(StringComparison.Ordinal))),
                v => v.ToList()));

        builder.Property<Guid>("BoardId");
        builder.HasIndex("BoardId");
    }
}
