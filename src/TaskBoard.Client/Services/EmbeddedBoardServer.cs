using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskBoard.Server.Hubs;
using TaskBoard.Server.Services;

namespace TaskBoard.Client.Services;

public sealed class EmbeddedBoardServer : IAsyncDisposable
{
    public const int DefaultPort = 5088;
    private WebApplication? app;

    public bool IsRunning => app is not null;
    public int Port { get; private set; } = DefaultPort;

    public async Task StartAsync(int port = DefaultPort, CancellationToken cancellationToken = default)
    {
        if (app is not null)
        {
            return;
        }

        Port = port;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<BoardStateService>();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowed(_ => true);
            });
        });

        app = builder.Build();
        app.UseCors();
        app.MapGet("/", () => "Team Task Board server is running. SignalR endpoint: /boardHub");
        app.MapHub<BoardHub>("/boardHub");
        await app.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (app is null)
        {
            return;
        }

        await app.StopAsync(cancellationToken);
        await app.DisposeAsync();
        app = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
