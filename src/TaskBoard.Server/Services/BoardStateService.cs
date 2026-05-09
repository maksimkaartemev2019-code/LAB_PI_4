using TaskBoard.Shared;

namespace TaskBoard.Server.Services;

public sealed class BoardStateService
{
    private readonly object syncRoot = new();
    private BoardState state = BoardState.CreateDefault();
    private readonly Dictionary<string, OnlineUser> users = [];

    public BoardState GetSnapshot()
    {
        lock (syncRoot)
        {
            return state.Clone();
        }
    }

    public IReadOnlyList<OnlineUser> GetUsers()
    {
        lock (syncRoot)
        {
            return users.Values
                .OrderBy(user => user.ConnectedAt)
                .Select(user => new OnlineUser
                {
                    ConnectionId = user.ConnectionId,
                    Name = user.Name,
                    ConnectedAt = user.ConnectedAt
                })
                .ToList();
        }
    }

    public void AddUser(string connectionId, string userName)
    {
        lock (syncRoot)
        {
            users[connectionId] = new OnlineUser
            {
                ConnectionId = connectionId,
                Name = string.IsNullOrWhiteSpace(userName) ? "Guest" : userName.Trim(),
                ConnectedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public void RemoveUser(string connectionId)
    {
        lock (syncRoot)
        {
            users.Remove(connectionId);
        }
    }

    public BoardState CreateColumn(string title)
    {
        lock (syncRoot)
        {
            state.Columns.Add(new BoardColumn
            {
                Title = NormalizeTitle(title, "New column"),
                SortOrder = state.Columns.Count
            });
            Touch();
            return state.Clone();
        }
    }

    public BoardState DeleteColumn(Guid columnId)
    {
        lock (syncRoot)
        {
            state.Columns.RemoveAll(column => column.Id == columnId);
            state.Cards.RemoveAll(card => card.ColumnId == columnId);
            NormalizeColumnOrder();
            Touch();
            return state.Clone();
        }
    }

    public BoardState CreateCard(Guid columnId, string title, string description)
    {
        lock (syncRoot)
        {
            if (state.Columns.All(column => column.Id != columnId))
            {
                return state.Clone();
            }

            state.Cards.Add(new TaskCard
            {
                ColumnId = columnId,
                Title = NormalizeTitle(title, "New task"),
                Description = description.Trim(),
                SortOrder = state.Cards.Count(card => card.ColumnId == columnId)
            });
            Touch();
            return state.Clone();
        }
    }

    public BoardState UpdateCard(CardUpdate update)
    {
        lock (syncRoot)
        {
            var card = state.Cards.FirstOrDefault(candidate => candidate.Id == update.Id);
            if (card is null)
            {
                return state.Clone();
            }

            card.Title = NormalizeTitle(update.Title, card.Title);
            card.Description = update.Description.Trim();
            card.UpdatedAt = DateTimeOffset.UtcNow;
            Touch();
            return state.Clone();
        }
    }

    public BoardState MoveCard(Guid cardId, Guid targetColumnId)
    {
        lock (syncRoot)
        {
            var card = state.Cards.FirstOrDefault(candidate => candidate.Id == cardId);
            if (card is null || state.Columns.All(column => column.Id != targetColumnId))
            {
                return state.Clone();
            }

            card.ColumnId = targetColumnId;
            card.SortOrder = state.Cards.Count(candidate => candidate.ColumnId == targetColumnId);
            card.UpdatedAt = DateTimeOffset.UtcNow;
            NormalizeCardOrder();
            Touch();
            return state.Clone();
        }
    }

    public BoardState DeleteCard(Guid cardId)
    {
        lock (syncRoot)
        {
            state.Cards.RemoveAll(card => card.Id == cardId);
            NormalizeCardOrder();
            Touch();
            return state.Clone();
        }
    }

    private static string NormalizeTitle(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private void Touch()
    {
        state.LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    private void NormalizeColumnOrder()
    {
        var ordered = state.Columns.OrderBy(column => column.SortOrder).ThenBy(column => column.Title).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].SortOrder = index;
        }

        state.Columns = ordered;
    }

    private void NormalizeCardOrder()
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
}
