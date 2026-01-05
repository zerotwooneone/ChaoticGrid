using ChaoticGrid.Server.Domain.Aggregates.BoardAggregate;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Entities;
using ChaoticGrid.Server.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChaoticGrid.Server.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Board> Boards => Set<Board>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RoleTemplate> RoleTemplates => Set<RoleTemplate>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Tile> Tiles => Set<Tile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BoardConfiguration());
        modelBuilder.ApplyConfiguration(new PlayerConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new RoleTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new TileConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
