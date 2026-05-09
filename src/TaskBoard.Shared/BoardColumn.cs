namespace TaskBoard.Shared;

public sealed class BoardColumn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
