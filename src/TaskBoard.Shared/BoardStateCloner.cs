namespace TaskBoard.Shared;

public static class BoardStateCloner
{
    public static BoardState Clone(this BoardState source)
    {
        return new BoardState
        {
            BoardName = source.BoardName,
            LastUpdatedAt = source.LastUpdatedAt,
            Columns = source.Columns
                .Select(column => new BoardColumn
                {
                    Id = column.Id,
                    Title = column.Title,
                    SortOrder = column.SortOrder
                })
                .ToList(),
            Cards = source.Cards
                .Select(card => new TaskCard
                {
                    Id = card.Id,
                    ColumnId = card.ColumnId,
                    Title = card.Title,
                    Description = card.Description,
                    SortOrder = card.SortOrder,
                    CreatedAt = card.CreatedAt,
                    UpdatedAt = card.UpdatedAt
                })
                .ToList()
        };
    }
}
