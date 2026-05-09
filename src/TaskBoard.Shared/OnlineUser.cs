namespace TaskBoard.Shared;

public sealed class OnlineUser
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.UtcNow;
}
