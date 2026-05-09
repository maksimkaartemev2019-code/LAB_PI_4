using TaskBoard.Client.Models;
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
        var viewModel = CreateViewModel(new FakeBoardClient(), new FakeCache(state), new LocalizationService());

        await viewModel.InitializeFromCacheAsync();

        Assert.Contains(viewModel.Columns, column => column.Title == "Acceptance");
    }

    [Fact]
    public void SwitchLanguage_ChangesVisibleLabels()
    {
        var localization = new LocalizationService();
        var viewModel = CreateViewModel(new FakeBoardClient(), new FakeCache(null), localization);

        viewModel.SwitchLanguageCommand.Execute("en");

        Assert.Equal("Team Task Board", viewModel.AppTitle);
    }

    [Fact]
    public async Task SwitchLanguage_DoesNotResetBoardOrStatus()
    {
        var viewModel = CreateViewModel(new FakeBoardClient(), new FakeCache(BoardState.CreateDefault()), new LocalizationService());
        await viewModel.InitializeFromCacheAsync();
        var status = viewModel.Status;
        var columnCount = viewModel.Columns.Count;

        viewModel.SwitchLanguageCommand.Execute("en");

        Assert.Equal(status, viewModel.Status);
        Assert.Equal(columnCount, viewModel.Columns.Count);
        Assert.Contains(viewModel.Columns, column => column.Title == "К выполнению");
    }

    [Fact]
    public async Task InitializeFromCache_LocalizesLegacyEnglishSeedData()
    {
        var legacy = new BoardState
        {
            BoardName = "Team Task Board",
            Columns =
            [
                new BoardColumn { Title = "To Do", SortOrder = 0 },
                new BoardColumn { Title = "In Progress", SortOrder = 1 },
                new BoardColumn { Title = "Done", SortOrder = 2 }
            ]
        };
        legacy.Cards.Add(new TaskCard
        {
            ColumnId = legacy.Columns[0].Id,
            Title = "Prepare backlog",
            Description = "Create first tasks and invite teammates."
        });
        var cache = new FakeCache(legacy);
        var viewModel = CreateViewModel(new FakeBoardClient(), cache, new LocalizationService());

        await viewModel.InitializeFromCacheAsync();

        Assert.Contains(viewModel.Columns, column => column.Title == "К выполнению");
        Assert.Contains(viewModel.Columns.SelectMany(column => column.Cards), card => card.Title == "Подготовить план задач");
        Assert.Equal("Совместная доска задач", cache.SavedState?.BoardName);
    }

    [Fact]
    public async Task AddColumnCommand_WorksWithoutServerConnection()
    {
        var cache = new FakeCache(BoardState.CreateDefault());
        var viewModel = CreateViewModel(new FakeBoardClient(), cache, new LocalizationService());
        await viewModel.InitializeFromCacheAsync();

        viewModel.NewColumnTitle = "Проверка";
        await viewModel.AddColumnCommand.ExecuteAsync(null);

        Assert.Contains(viewModel.Columns, column => column.Title == "Проверка");
        Assert.NotNull(cache.SavedState);
    }

    [Fact]
    public async Task AddAndDeleteCardCommands_WorkWithoutServerConnection()
    {
        var viewModel = CreateViewModel(new FakeBoardClient(), new FakeCache(BoardState.CreateDefault()), new LocalizationService());
        await viewModel.InitializeFromCacheAsync();
        var column = viewModel.Columns[0];

        await viewModel.AddCardCommand.ExecuteAsync(column);
        var card = Assert.Single(viewModel.Columns.SelectMany(item => item.Cards), item => item.Title == "Заголовок");

        viewModel.SelectCardCommand.Execute(card);
        await viewModel.DeleteCardCommand.ExecuteAsync(null);

        Assert.DoesNotContain(viewModel.Columns.SelectMany(item => item.Cards), item => item.Id == card.Id);
    }

    [Fact]
    public void SelectedDiscoveredHost_UpdatesServerUrl()
    {
        var viewModel = CreateViewModel(new FakeBoardClient(), new FakeCache(null), new LocalizationService());
        var host = new DiscoveredBoardHost
        {
            Name = "Office PC",
            Url = "http://192.168.1.10:5088"
        };

        viewModel.SelectedHost = host;

        Assert.Equal("http://192.168.1.10:5088", viewModel.ServerUrl);
    }

    private static MainWindowViewModel CreateViewModel(
        IBoardRealtimeClient client,
        ILocalBoardCache cache,
        LocalizationService localization)
    {
        return new MainWindowViewModel(
            client,
            cache,
            localization,
            new EmbeddedBoardServer(),
            new LanDiscoveryService(),
            startDiscovery: false);
    }

    private sealed class FakeCache(BoardState? state) : ILocalBoardCache
    {
        public BoardState? SavedState { get; private set; }

        public Task<BoardState?> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(state);
        }

        public Task SaveAsync(BoardState state, CancellationToken cancellationToken = default)
        {
            SavedState = state;
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
