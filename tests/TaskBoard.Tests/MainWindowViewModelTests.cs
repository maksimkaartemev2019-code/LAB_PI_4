using TaskBoard.Client.Services;
using TaskBoard.Client.ViewModels;
using TaskBoard.Shared;

namespace TaskBoard.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task InitializeFromCache_LoadsCachedColumns()
    {
        var state = BoardState.CreateDefault();
        state.Columns.Add(new BoardColumn { Title = "Acceptance", SortOrder = 10 });
        var viewModel = new MainWindowViewModel(new FakeBoardClient(), new FakeCache(state), new LocalizationService());

        await viewModel.InitializeFromCacheAsync();

        Assert.Contains(viewModel.Columns, column => column.Title == "Acceptance");
    }

    [Fact]
    public void SwitchLanguage_ChangesVisibleLabels()
    {
        var localization = new LocalizationService();
        var viewModel = new MainWindowViewModel(new FakeBoardClient(), new FakeCache(null), localization);

        viewModel.SwitchLanguageCommand.Execute("en");

        Assert.Equal("Team Task Board", viewModel.AppTitle);
    }

    private sealed class FakeCache(BoardState? state) : ILocalBoardCache
    {
        public Task<BoardState?> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(state);
        }

        public Task SaveAsync(BoardState state, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBoardClient : IBoardRealtimeClient
    {
        public event Action<BoardState>? BoardUpdated;
        public event Action<IReadOnlyList<OnlineUser>>? UsersUpdated;
        public event Action<bool>? ConnectionChanged;

        public bool IsConnected { get; private set; }

        public Task ConnectAsync(string serverUrl, string userName, CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            ConnectionChanged?.Invoke(true);
            BoardUpdated?.Invoke(BoardState.CreateDefault());
            UsersUpdated?.Invoke([new OnlineUser { Name = userName }]);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            ConnectionChanged?.Invoke(false);
            return Task.CompletedTask;
        }

        public Task CreateColumnAsync(string title, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteColumnAsync(Guid columnId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CreateCardAsync(Guid columnId, string title, string description, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateCardAsync(CardUpdate update, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MoveCardAsync(Guid cardId, Guid targetColumnId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteCardAsync(Guid cardId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
