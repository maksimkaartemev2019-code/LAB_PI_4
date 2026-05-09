using Microsoft.AspNetCore.SignalR.Client;
using TaskBoard.Shared;

namespace TaskBoard.Client.Services;

public sealed class BoardHubClient : IBoardRealtimeClient
{
    private HubConnection? connection;

    public event Action<BoardState>? BoardUpdated;
    public event Action<IReadOnlyList<OnlineUser>>? UsersUpdated;
    public event Action<bool>? ConnectionChanged;

    public bool IsConnected => connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string serverUrl, string userName, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            return;
        }

        var hubUrl = $"{serverUrl.TrimEnd('/')}/boardHub";
        connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        connection.On<BoardState>("BoardUpdated", state => BoardUpdated?.Invoke(state));
        connection.On<IReadOnlyList<OnlineUser>>("UsersUpdated", users => UsersUpdated?.Invoke(users));
        connection.Reconnected += _ =>
        {
            ConnectionChanged?.Invoke(true);
            return Task.CompletedTask;
        };
        connection.Closed += _ =>
        {
            ConnectionChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        await connection.StartAsync(cancellationToken);
        ConnectionChanged?.Invoke(true);
        await connection.InvokeAsync("JoinAsync", userName, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null)
        {
            return;
        }

        await connection.StopAsync(cancellationToken);
        await connection.DisposeAsync();
        connection = null;
        ConnectionChanged?.Invoke(false);
    }

    public Task CreateColumnAsync(string title, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("CreateColumnAsync", cancellationToken, title);
    }

    public Task DeleteColumnAsync(Guid columnId, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("DeleteColumnAsync", cancellationToken, columnId);
    }

    public Task CreateCardAsync(Guid columnId, string title, string description, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("CreateCardAsync", cancellationToken, columnId, title, description);
    }

    public Task UpdateCardAsync(CardUpdate update, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("UpdateCardAsync", cancellationToken, update);
    }

    public Task MoveCardAsync(Guid cardId, Guid targetColumnId, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("MoveCardAsync", cancellationToken, cardId, targetColumnId);
    }

    public Task DeleteCardAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return InvokeAsync("DeleteCardAsync", cancellationToken, cardId);
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    private Task InvokeAsync(string methodName, CancellationToken cancellationToken, params object?[] args)
    {
        if (connection is null)
        {
            throw new InvalidOperationException("The board client is not connected.");
        }

        return connection.InvokeCoreAsync(methodName, args, cancellationToken);
    }
}
