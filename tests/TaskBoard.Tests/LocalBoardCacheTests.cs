using TaskBoard.Client.Services;
using TaskBoard.Shared;

namespace TaskBoard.Tests;

public sealed class LocalBoardCacheTests
{
    [Fact]
    public async Task Cache_RoundTripsBoardState()
    {
        var path = Path.Combine(Path.GetTempPath(), $"task-board-{Guid.NewGuid():N}.json");
        var cache = new LocalBoardCache(path);
        var expected = BoardState.CreateDefault();
        expected.Columns.Add(new BoardColumn { Title = "QA", SortOrder = 99 });

        await cache.SaveAsync(expected);
        var actual = await cache.LoadAsync();

        Assert.NotNull(actual);
        Assert.Contains(actual!.Columns, column => column.Title == "QA");
        File.Delete(path);
    }
}
