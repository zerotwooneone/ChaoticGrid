using ChaoticGrid.Server.Domain.Interfaces;
using ChaoticGrid.Server.Domain.Services;
using ChaoticGrid.Server.Infrastructure.Persistence;
using ChaoticGrid.Server.Infrastructure.Persistence.Repositories;
using ChaoticGrid.Server.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChaoticGrid.Server.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=chaoticgrid.db";

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IBoardRepository, SqliteBoardRepository>();
        services.AddScoped<IUserRepository, SqliteUserRepository>();

        services.AddSingleton<GridGeneratorService>();
        services.AddSingleton<MatchManager>();

        services.AddSingleton<JwtTokenGenerator>();
        services.AddSingleton<InitialSetupService>();
        services.AddHostedService(sp => sp.GetRequiredService<InitialSetupService>());
        services.AddSingleton<InviteService>();

        return services;
    }
}
