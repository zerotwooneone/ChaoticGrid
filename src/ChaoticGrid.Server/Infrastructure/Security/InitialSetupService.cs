using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Infrastructure.Persistence;
using ChaoticGrid.Server.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChaoticGrid.Server.Infrastructure.Security;

public sealed class InitialSetupService(
    IServiceProvider serviceProvider,
    IHostEnvironment hostEnvironment,
    ILogger<InitialSetupService> logger)
    : IHostedService
{
    private string TokenDirectoryPath => Path.Combine(hostEnvironment.ContentRootPath, "app_data");

    private string TokenFilePath => Path.Combine(TokenDirectoryPath, "setup-token.txt");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        var userCount = await db.Set<User>().CountAsync(cancellationToken);
        if (userCount != 0)
        {
            return;
        }

        if (File.Exists(TokenFilePath))
        {
            return;
        }

        Directory.CreateDirectory(TokenDirectoryPath);

        var token = Guid.NewGuid().ToString("D");
        try
        {
            await using var stream = new FileStream(TokenFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(token.AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);

            logger.LogWarning("Initial setup required. Setup token written to {TokenFilePath}", TokenFilePath);
        }
        catch (IOException)
        {
            // Another process/test host likely created the token concurrently.
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<string?> TryClaimSetup(string token, string nickname, JwtTokenGenerator jwtTokenGenerator, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(nickname))
        {
            return null;
        }

        if (!File.Exists(TokenFilePath))
        {
            return null;
        }

        string expected;
        try
        {
            expected = (await File.ReadAllTextAsync(TokenFilePath, cancellationToken)).Trim();
        }
        catch (IOException)
        {
            return null;
        }

        if (!string.Equals(expected, token.Trim(), StringComparison.Ordinal))
        {
            return null;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        var adminRole = await db.Set<Role>()
            .FirstOrDefaultAsync(r => r.Id == RoleConfiguration.AdminRoleId, cancellationToken);

        if (adminRole is null)
        {
            return null;
        }

        var user = new User(Guid.NewGuid(), nickname);
        user.GlobalRoles.Add(adminRole);

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            File.Delete(TokenFilePath);
        }
        catch (IOException)
        {
            // Ignore; token can be deleted later.
        }

        return jwtTokenGenerator.Generate(user);
    }
}
