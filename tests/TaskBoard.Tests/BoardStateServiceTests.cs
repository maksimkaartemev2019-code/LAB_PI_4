using TaskBoard.Server.Services;
using TaskBoard.Shared;

namespace TaskBoard.Tests;

public sealed class BoardStateServiceTests
{
    [Fact]
    public void CreateDefault_UsesRussianSeedData()
    {
        var state = BoardState.CreateDefault();

        Assert.Equal("Совместная доска задач", state.BoardName);
        Assert.Contains(state.Columns, column => column.Title == "К выполнению");
        Assert.Contains(state.Columns, column => column.Title == "В работе");
        Assert.Contains(state.Columns, column => column.Title == "Готово");
        Assert.Contains(state.Cards, card => card.Title == "Подготовить план задач");
    }

    [Fact]
    public void CreateColumn_AddsColumnToSnapshot()
    {
        var service = new BoardStateService();

        var state = service.CreateColumn("Review");

        Assert.Contains(state.Columns, column => column.Title == "Review");
    }

    [Fact]
    public void DeleteColumn_RemovesColumnCards()
    {
        var service = new BoardStateService();
        var column = service.CreateColumn("Blocked").Columns.Single(item => item.Title == "Blocked");
        var withCard = service.CreateCard(column.Id, "Fix build", "Check CI");
        var card = withCard.Cards.Single(item => item.ColumnId == column.Id);

        var state = service.DeleteColumn(column.Id);

        Assert.DoesNotContain(state.Columns, item => item.Id == column.Id);
        Assert.DoesNotContain(state.Cards, item => item.Id == card.Id);
    }

    [Fact]
    public void MoveCard_ChangesCardColumn()
    {
        var service = new BoardStateService();
        var first = service.GetSnapshot().Columns[0];
        var second = service.GetSnapshot().Columns[1];
        var card = service.CreateCard(first.Id, "Move me", string.Empty).Cards.Last();

        var state = service.MoveCard(card.Id, second.Id);

        Assert.Equal(second.Id, state.Cards.Single(item => item.Id == card.Id).ColumnId);
    }

    [Fact]
    public void Users_AreTrackedByConnectionId()
    {
        var service = new BoardStateService();

        service.AddUser("a", "Anna");
        service.AddUser("b", "Ben");
        service.RemoveUser("a");

        var user = Assert.Single(service.GetUsers());
        Assert.Equal("Ben", user.Name);
    }
}
