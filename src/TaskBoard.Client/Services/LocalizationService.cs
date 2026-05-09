using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskBoard.Client.Services;

public sealed partial class LocalizationService : ObservableObject
{
    private readonly Dictionary<string, Dictionary<string, string>> resources = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "Team Task Board",
            ["Server"] = "Server",
            ["User"] = "User",
            ["Connect"] = "Connect",
            ["Disconnect"] = "Disconnect",
            ["StartServer"] = "Start server",
            ["StopServer"] = "Stop server",
            ["DiscoveredHosts"] = "Found servers",
            ["LocalServer"] = "local server",
            ["ServerStarted"] = "Server started",
            ["ServerStopped"] = "Server stopped",
            ["ColumnPlaceholder"] = "Column title",
            ["AddColumn"] = "Add column",
            ["Online"] = "Online",
            ["Details"] = "Task details",
            ["Title"] = "Title",
            ["Description"] = "Description",
            ["Save"] = "Save",
            ["DeleteCard"] = "Delete task",
            ["DeleteColumn"] = "Delete column",
            ["AddCard"] = "Add task",
            ["MoveLeft"] = "Left",
            ["MoveRight"] = "Right",
            ["Language"] = "Language",
            ["Ready"] = "Ready",
            ["Offline"] = "Offline cache loaded",
            ["Connected"] = "Connected",
            ["Disconnected"] = "Disconnected",
            ["ConnectFirst"] = "Connect to the server first."
        },
        ["ru"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "Совместная доска задач",
            ["Server"] = "Сервер",
            ["User"] = "Пользователь",
            ["Connect"] = "Подключиться",
            ["Disconnect"] = "Отключиться",
            ["StartServer"] = "Запустить сервер",
            ["StopServer"] = "Остановить сервер",
            ["DiscoveredHosts"] = "Найденные серверы",
            ["LocalServer"] = "локальный сервер",
            ["ServerStarted"] = "Сервер запущен",
            ["ServerStopped"] = "Сервер остановлен",
            ["ColumnPlaceholder"] = "Название колонки",
            ["AddColumn"] = "Добавить колонку",
            ["Online"] = "Онлайн",
            ["Details"] = "Детали задачи",
            ["Title"] = "Заголовок",
            ["Description"] = "Описание",
            ["Save"] = "Сохранить",
            ["DeleteCard"] = "Удалить задачу",
            ["DeleteColumn"] = "Удалить колонку",
            ["AddCard"] = "Добавить задачу",
            ["MoveLeft"] = "Влево",
            ["MoveRight"] = "Вправо",
            ["Language"] = "Язык",
            ["Ready"] = "Готово",
            ["Offline"] = "Загружена офлайн-копия",
            ["Connected"] = "Подключено",
            ["Disconnected"] = "Отключено",
            ["ConnectFirst"] = "Сначала подключитесь к серверу."
        }
    };

    [ObservableProperty]
    private string language = "ru";

    public string this[string key] => resources[Language].GetValueOrDefault(key, key);

    partial void OnLanguageChanged(string value)
    {
        if (!resources.ContainsKey(value))
        {
            Language = "ru";
            return;
        }

        OnPropertyChanged("Item[]");
    }
}
