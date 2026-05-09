using Microsoft.AspNetCore.SignalR;
using TaskBoard.Server.Services;
using TaskBoard.Shared;

namespace TaskBoard.Server.Hubs;

public sealed class BoardHub(BoardStateService boardState) : Hub
{
    public Task<BoardState> GetBoardAsync()
    {
        return Task.FromResult(boardState.GetSnapshot());
    }

    public async Task JoinAsync(string userName)
    {
        boardState.AddUser(Context.ConnectionId, userName);
        await Clients.Caller.SendAsync("BoardUpdated", boardState.GetSnapshot());
        await Clients.All.SendAsync("UsersUpdated", boardState.GetUsers());
    }

    public async Task CreateColumnAsync(string title)
    {
        await BroadcastBoardAsync(boardState.CreateColumn(title));
    }

    public async Task DeleteColumnAsync(Guid columnId)
    {
        await BroadcastBoardAsync(boardState.DeleteColumn(columnId));
    }

    public async Task CreateCardAsync(Guid columnId, string title, string description)
    {
        await BroadcastBoardAsync(boardState.CreateCard(columnId, title, description));
    }

    public async Task UpdateCardAsync(CardUpdate update)
    {
        await BroadcastBoardAsync(boardState.UpdateCard(update));
    }

    public async Task MoveCardAsync(Guid cardId, Guid targetColumnId)
    {
        await BroadcastBoardAsync(boardState.MoveCard(cardId, targetColumnId));
    }

    public async Task DeleteCardAsync(Guid cardId)
    {
        await BroadcastBoardAsync(boardState.DeleteCard(cardId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        boardState.RemoveUser(Context.ConnectionId);
        await Clients.All.SendAsync("UsersUpdated", boardState.GetUsers());
        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastBoardAsync(BoardState snapshot)
    {
        return Clients.All.SendAsync("BoardUpdated", snapshot);
    }
}
