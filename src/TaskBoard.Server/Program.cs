using TaskBoard.Server.Hubs;
using TaskBoard.Server.Services;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.UseCors();
app.MapGet("/", () => Results.Ok("Team Task Board server is running. SignalR endpoint: /boardHub"));
app.MapHub<BoardHub>("/boardHub");

app.Run();
