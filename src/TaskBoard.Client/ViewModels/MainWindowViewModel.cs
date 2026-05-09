using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskBoard.Client.Services;
using TaskBoard.Shared;

namespace TaskBoard.Client.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IBoardRealtimeClient client;
    private readonly ILocalBoardCache cache;
    private readonly LocalizationService localization;
    private CardViewModel? selectedCard;
    private string serverUrl = "http://localhost:5000";
    private string userName = Environment.UserName;
    private string newColumnTitle = string.Empty;
    private string status;
    private bool isConnected;
    private BoardState currentState = BoardState.CreateDefault();

    public MainWindowViewModel()
        : this(new BoardHubClient(), new LocalBoardCache(), new LocalizationService())
    {
    }

    public MainWindowViewModel(
        IBoardRealtimeClient client,
        ILocalBoardCache cache,
        LocalizationService localization)
    {
        this.client = client;
        this.cache = cache;
        this.localization = localization;
        status = localization["Ready"];

        ConnectCommand = new AsyncRelayCommand(ConnectOrDisconnectAsync);
        AddColumnCommand = new AsyncRelayCommand(AddColumnAsync);
        DeleteColumnCommand = new AsyncRelayCommand<ColumnViewModel>(DeleteColumnAsync);
        AddCardCommand = new AsyncRelayCommand<ColumnViewModel>(AddCardAsync);
        SelectCardCommand = new RelayCommand<CardViewModel>(card => SelectedCard = card);
        SaveCardCommand = new AsyncRelayCommand(SaveSelectedCardAsync);
        DeleteCardCommand = new AsyncRelayCommand(DeleteSelectedCardAsync);
        MoveCardLeftCommand = new AsyncRelayCommand(MoveSelectedCardLeftAsync);
        MoveCardRightCommand = new AsyncRelayCommand(MoveSelectedCardRightAsync);
        SwitchLanguageCommand = new RelayCommand<string>(SwitchLanguage);

        localization.PropertyChanged += (_, _) => RaiseLocalizedProperties();
        client.BoardUpdated += OnBoardUpdated;
        client.UsersUpdated += OnUsersUpdated;
        client.ConnectionChanged += connected => RunOnUiThread(() => IsConnected = connected);
    }

    public ObservableCollection<ColumnViewModel> Columns { get; } = [];
    public ObservableCollection<string> OnlineUsers { get; } = [];

    public IAsyncRelayCommand ConnectCommand { get; }
    public IAsyncRelayCommand AddColumnCommand { get; }
    public IAsyncRelayCommand<ColumnViewModel> DeleteColumnCommand { get; }
    public IAsyncRelayCommand<ColumnViewModel> AddCardCommand { get; }
    public IRelayCommand<CardViewModel> SelectCardCommand { get; }
    public IAsyncRelayCommand SaveCardCommand { get; }
    public IAsyncRelayCommand DeleteCardCommand { get; }
    public IAsyncRelayCommand MoveCardLeftCommand { get; }
    public IAsyncRelayCommand MoveCardRightCommand { get; }
    public IRelayCommand<string> SwitchLanguageCommand { get; }

    public string ServerUrl
    {
        get => serverUrl;
        set => SetProperty(ref serverUrl, value);
    }

    public string UserName
    {
        get => userName;
        set => SetProperty(ref userName, value);
    }

    public string NewColumnTitle
    {
        get => newColumnTitle;
        set => SetProperty(ref newColumnTitle, value);
    }

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    public bool IsConnected
    {
        get => isConnected;
        private set
        {
            if (SetProperty(ref isConnected, value))
            {
                Status = value ? localization["Connected"] : localization["Disconnected"];
                OnPropertyChanged(nameof(ConnectButtonLabel));
            }
        }
    }

    public CardViewModel? SelectedCard
    {
        get => selectedCard;
        set
        {
            if (SetProperty(ref selectedCard, value))
            {
                OnPropertyChanged(nameof(HasSelectedCard));
            }
        }
    }

    public bool HasSelectedCard => SelectedCard is not null;
    public string CurrentLanguage => localization.Language.ToUpperInvariant();
    public string AppTitle => localization["AppTitle"];
    public string ServerLabel => localization["Server"];
    public string UserLabel => localization["User"];
    public string ConnectButtonLabel => IsConnected ? localization["Disconnect"] : localization["Connect"];
    public string ColumnPlaceholder => localization["ColumnPlaceholder"];
    public string AddColumnLabel => localization["AddColumn"];
    public string OnlineLabel => localization["Online"];
    public string DetailsLabel => localization["Details"];
    public string TitleLabel => localization["Title"];
    public string DescriptionLabel => localization["Description"];
    public string SaveLabel => localization["Save"];
    public string DeleteCardLabel => localization["DeleteCard"];
    public string DeleteColumnLabel => localization["DeleteColumn"];
    public string AddCardLabel => localization["AddCard"];
    public string MoveLeftLabel => localization["MoveLeft"];
    public string MoveRightLabel => localization["MoveRight"];
    public string LanguageLabel => localization["Language"];

    public async Task InitializeFromCacheAsync()
    {
        var cached = await cache.LoadAsync();
        if (cached is not null)
        {
            ApplyBoard(cached);
            Status = localization["Offline"];
        }
        else
        {
            ApplyBoard(BoardState.CreateDefault());
        }
    }

    private async Task ConnectOrDisconnectAsync()
    {
        try
        {
            if (IsConnected)
            {
                await client.DisconnectAsync();
                return;
            }

            await client.ConnectAsync(ServerUrl, UserName);
        }
        catch (Exception ex)
        {
            IsConnected = false;
            Status = ex.Message;
        }
    }

    private async Task AddColumnAsync()
    {
        var title = string.IsNullOrWhiteSpace(NewColumnTitle) ? localization["ColumnPlaceholder"] : NewColumnTitle;
        if (IsConnected)
        {
            await client.CreateColumnAsync(title);
            NewColumnTitle = string.Empty;
            return;
        }

        await ApplyLocalChangeAsync(state =>
        {
            state.Columns.Add(new BoardColumn
            {
                Title = title,
                SortOrder = state.Columns.Count
            });
        });
        NewColumnTitle = string.Empty;
    }

    private async Task DeleteColumnAsync(ColumnViewModel? column)
    {
        if (column is null)
        {
            return;
        }

        if (IsConnected)
        {
            await client.DeleteColumnAsync(column.Id);
            return;
        }

        await ApplyLocalChangeAsync(state =>
        {
            state.Columns.RemoveAll(item => item.Id == column.Id);
            state.Cards.RemoveAll(card => card.ColumnId == column.Id);
            NormalizeColumnOrder(state);
        });
    }

    private async Task AddCardAsync(ColumnViewModel? column)
    {
        if (column is null)
        {
            return;
        }

        if (IsConnected)
        {
            await client.CreateCardAsync(column.Id, localization["Title"], string.Empty);
            return;
        }

        await ApplyLocalChangeAsync(state =>
        {
            state.Cards.Add(new TaskCard
            {
                ColumnId = column.Id,
                Title = localization["Title"],
                Description = string.Empty,
                SortOrder = state.Cards.Count(card => card.ColumnId == column.Id)
            });
        });
    }

    private async Task SaveSelectedCardAsync()
    {
        if (SelectedCard is null)
        {
            return;
        }

        var update = new CardUpdate
        {
            Id = SelectedCard.Id,
            Title = SelectedCard.Title,
            Description = SelectedCard.Description
        };

        if (IsConnected)
        {
            await client.UpdateCardAsync(update);
            return;
        }

        await ApplyLocalChangeAsync(state =>
        {
            var card = state.Cards.FirstOrDefault(item => item.Id == update.Id);
            if (card is null)
            {
                return;
            }

            card.Title = string.IsNullOrWhiteSpace(update.Title) ? localization["Title"] : update.Title.Trim();
            card.Description = update.Description.Trim();
            card.UpdatedAt = DateTimeOffset.UtcNow;
        });
    }

    private async Task DeleteSelectedCardAsync()
    {
        if (SelectedCard is null)
        {
            return;
        }

        var cardId = SelectedCard.Id;
        SelectedCard = null;
        if (IsConnected)
        {
            await client.DeleteCardAsync(cardId);
            return;
        }

        await ApplyLocalChangeAsync(state =>
        {
            state.Cards.RemoveAll(card => card.Id == cardId);
            NormalizeCardOrder(state);
        });
    }

    private Task MoveSelectedCardLeftAsync()
    {
        return MoveSelectedCardAsync(-1);
    }

    private Task MoveSelectedCardRightAsync()
    {
        return MoveSelectedCardAsync(1);
    }

    private async Task MoveSelectedCardAsync(int offset)
    {
        if (SelectedCard is null)
        {
            return;
        }

        var currentColumn = Columns.FirstOrDefault(column => column.Id == SelectedCard.ColumnId);
        if (currentColumn is null)
        {
            return;
        }

        var currentIndex = Columns.IndexOf(currentColumn);
        var targetIndex = currentIndex + offset;
        if (targetIndex < 0 || targetIndex >= Columns.Count)
        {
            return;
        }

        var cardId = SelectedCard.Id;
        var targetColumnId = Columns[targetIndex].Id;
        if (IsConnected)
        {
            await client.MoveCardAsync(cardId, targetColumnId);
            return;
        }

        await ApplyLocalChangeAsync(state =>
        {
            var card = state.Cards.FirstOrDefault(item => item.Id == cardId);
            if (card is null)
            {
                return;
            }

            card.ColumnId = targetColumnId;
            card.SortOrder = state.Cards.Count(item => item.ColumnId == targetColumnId);
            card.UpdatedAt = DateTimeOffset.UtcNow;
            NormalizeCardOrder(state);
        });
    }

    private void SwitchLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return;
        }

        localization.Language = language;
        Status = IsConnected ? localization["Connected"] : localization["Ready"];
    }

    private void OnBoardUpdated(BoardState state)
    {
        RunOnUiThread(() => ApplyBoard(state));
        _ = cache.SaveAsync(state);
    }

    private void OnUsersUpdated(IReadOnlyList<OnlineUser> users)
    {
        RunOnUiThread(() =>
        {
            OnlineUsers.Clear();
            foreach (var user in users)
            {
                OnlineUsers.Add(user.Name);
            }
        });
    }

    private void ApplyBoard(BoardState state)
    {
        currentState = state.Clone();
        var selectedId = SelectedCard?.Id;
        Columns.Clear();
        foreach (var column in state.Columns.OrderBy(column => column.SortOrder))
        {
            var cards = state.Cards
                .Where(card => card.ColumnId == column.Id)
                .OrderBy(card => card.SortOrder)
                .ThenBy(card => card.CreatedAt);
            Columns.Add(new ColumnViewModel(column, cards));
        }

        SelectedCard = Columns
            .SelectMany(column => column.Cards)
            .FirstOrDefault(card => card.Id == selectedId);
    }

    private async Task ApplyLocalChangeAsync(Action<BoardState> change)
    {
        var state = currentState.Clone();
        change(state);
        state.LastUpdatedAt = DateTimeOffset.UtcNow;
        ApplyBoard(state);
        await cache.SaveAsync(state);
        Status = localization["Offline"];
    }

    private static void NormalizeColumnOrder(BoardState state)
    {
        var ordered = state.Columns.OrderBy(column => column.SortOrder).ThenBy(column => column.Title).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].SortOrder = index;
        }

        state.Columns = ordered;
    }

    private static void NormalizeCardOrder(BoardState state)
    {
        foreach (var group in state.Cards.GroupBy(card => card.ColumnId))
        {
            var ordered = group.OrderBy(card => card.SortOrder).ThenBy(card => card.CreatedAt).ToList();
            for (var index = 0; index < ordered.Count; index++)
            {
                ordered[index].SortOrder = index;
            }
        }
    }

    private void RaiseLocalizedProperties()
    {
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged(nameof(AppTitle));
        OnPropertyChanged(nameof(ServerLabel));
        OnPropertyChanged(nameof(UserLabel));
        OnPropertyChanged(nameof(ConnectButtonLabel));
        OnPropertyChanged(nameof(ColumnPlaceholder));
        OnPropertyChanged(nameof(AddColumnLabel));
        OnPropertyChanged(nameof(OnlineLabel));
        OnPropertyChanged(nameof(DetailsLabel));
        OnPropertyChanged(nameof(TitleLabel));
        OnPropertyChanged(nameof(DescriptionLabel));
        OnPropertyChanged(nameof(SaveLabel));
        OnPropertyChanged(nameof(DeleteCardLabel));
        OnPropertyChanged(nameof(DeleteColumnLabel));
        OnPropertyChanged(nameof(AddCardLabel));
        OnPropertyChanged(nameof(MoveLeftLabel));
        OnPropertyChanged(nameof(MoveRightLabel));
        OnPropertyChanged(nameof(LanguageLabel));
    }

    private static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
    }
}
