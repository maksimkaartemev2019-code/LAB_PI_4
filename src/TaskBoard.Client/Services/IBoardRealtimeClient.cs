using TaskBoard.Shared;

namespace TaskBoard.Client.Services;

public interface IBoardRealtimeClient : IAsyncDisposable
{
    event Action<BoardState>? BoardUpdated;
    event Action<IReadOnlyList<OnlineUser>>? UsersUpdated;
    event Action<bool>? ConnectionChanged;

    bool IsConnected { get; }

    Task ConnectAsync(string serverUrl, string userName, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task CreateColumnAsync(string title, CancellationToken cancellationToken = default);
    Task DeleteColumnAsync(Guid columnId, CancellationToken cancellationToken = default);
    Task CreateCardAsync(Guid columnId, string title, string description, CancellationToken cancellationToken = default);
    Task UpdateCardAsync(CardUpdate update, CancellationToken cancellationToken = default);
    Task MoveCardAsync(Guid cardId, Guid targetColumnId, CancellationToken cancellationToken = default);
    Task DeleteCardAsync(Guid cardId, CancellationToken cancellationToken = default);
}
