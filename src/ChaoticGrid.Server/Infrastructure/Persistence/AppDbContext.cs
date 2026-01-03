using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChaoticGrid.Server.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Board> Boards => Set<Board>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Tile> Tiles => Set<Tile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BoardConfiguration());
        modelBuilder.ApplyConfiguration(new PlayerConfiguration());
        modelBuilder.ApplyConfiguration(new TileConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
