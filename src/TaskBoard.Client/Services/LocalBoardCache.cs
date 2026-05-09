using System.Text.Json;
using TaskBoard.Shared;

namespace TaskBoard.Client.Services;

public sealed class LocalBoardCache : ILocalBoardCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string filePath;

    public LocalBoardCache()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TeamTaskBoard",
            "board-cache.json"))
    {
    }

    public LocalBoardCache(string filePath)
    {
        this.filePath = filePath;
    }

    public async Task<BoardState?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<BoardState>(stream, SerializerOptions, cancellationToken);
    }

    public async Task SaveAsync(BoardState state, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);
    }
}
