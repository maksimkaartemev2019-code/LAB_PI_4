namespace TaskBoard.Shared;

public sealed class BoardState
{
    public string BoardName { get; set; } = "Совместная доска задач";
    public List<BoardColumn> Columns { get; set; } = [];
    public List<TaskCard> Cards { get; set; } = [];
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static BoardState CreateDefault()
    {
        var todo = new BoardColumn { Title = "К выполнению", SortOrder = 0 };
        var progress = new BoardColumn { Title = "В работе", SortOrder = 1 };
        var done = new BoardColumn { Title = "Готово", SortOrder = 2 };

        return new BoardState
        {
            Columns = [todo, progress, done],
            Cards =
            [
                new TaskCard
                {
                    ColumnId = todo.Id,
                    Title = "Подготовить план задач",
                    Description = "Создать первые карточки и пригласить участников.",
                    SortOrder = 0
                }
            ],
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
