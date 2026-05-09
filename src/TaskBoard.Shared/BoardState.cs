namespace TaskBoard.Shared;

public sealed class BoardState
{
    public string BoardName { get; set; } = "Team Task Board";
    public List<BoardColumn> Columns { get; set; } = [];
    public List<TaskCard> Cards { get; set; } = [];
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static BoardState CreateDefault()
    {
        var todo = new BoardColumn { Title = "To Do", SortOrder = 0 };
        var progress = new BoardColumn { Title = "In Progress", SortOrder = 1 };
        var done = new BoardColumn { Title = "Done", SortOrder = 2 };

        return new BoardState
        {
            Columns = [todo, progress, done],
            Cards =
            [
                new TaskCard
                {
                    ColumnId = todo.Id,
                    Title = "Prepare backlog",
                    Description = "Create first tasks and invite teammates.",
                    SortOrder = 0
                }
            ],
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
