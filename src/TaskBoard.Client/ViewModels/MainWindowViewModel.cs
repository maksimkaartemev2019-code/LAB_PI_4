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
        if (!EnsureConnected())
        {
            return;
        }

        await client.CreateColumnAsync(NewColumnTitle);
        NewColumnTitle = string.Empty;
    }

    private async Task DeleteColumnAsync(ColumnViewModel? column)
    {
        if (column is null || !EnsureConnected())
        {
            return;
        }

        await client.DeleteColumnAsync(column.Id);
    }

    private async Task AddCardAsync(ColumnViewModel? column)
    {
        if (column is null || !EnsureConnected())
        {
            return;
        }

        await client.CreateCardAsync(column.Id, localization["Title"], string.Empty);
    }

    private async Task SaveSelectedCardAsync()
    {
        if (SelectedCard is null || !EnsureConnected())
        {
            return;
        }

        await client.UpdateCardAsync(new CardUpdate
        {
            Id = SelectedCard.Id,
            Title = SelectedCard.Title,
            Description = SelectedCard.Description
        });
    }

    private async Task DeleteSelectedCardAsync()
    {
        if (SelectedCard is null || !EnsureConnected())
        {
            return;
        }

        var cardId = SelectedCard.Id;
        SelectedCard = null;
        await client.DeleteCardAsync(cardId);
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
        if (SelectedCard is null || !EnsureConnected())
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

        await client.MoveCardAsync(SelectedCard.Id, Columns[targetIndex].Id);
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

    private bool EnsureConnected()
    {
        if (IsConnected)
        {
            return true;
        }

        Status = localization["ConnectFirst"];
        return false;
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
