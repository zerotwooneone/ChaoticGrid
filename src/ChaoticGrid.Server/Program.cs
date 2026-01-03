using ChaoticGrid.Server.Api.Endpoints;
using ChaoticGrid.Server.Infrastructure.Extensions;
using ChaoticGrid.Server.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSignalR();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapBoardEndpoints();

app.MapHub<GameHub>("/hubs/game");

app.Run();
