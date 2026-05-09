using TaskBoard.Shared;

namespace TaskBoard.Client.Services;

public interface ILocalBoardCache
{
    Task<BoardState?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(BoardState state, CancellationToken cancellationToken = default);
}
