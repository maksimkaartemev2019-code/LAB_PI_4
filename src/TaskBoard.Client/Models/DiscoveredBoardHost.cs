namespace TaskBoard.Client.Models;

public sealed class DiscoveredBoardHost
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Url : $"{Name} ({Url})";
}
