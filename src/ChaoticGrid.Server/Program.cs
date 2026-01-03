using ChaoticGrid.Server.Api.Endpoints;
using ChaoticGrid.Server.Infrastructure.Extensions;
using ChaoticGrid.Server.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("Default");

app.MapBoardEndpoints();

app.MapHub<GameHub>("/hubs/game");

app.MapFallbackToFile("index.html");

app.Run();
