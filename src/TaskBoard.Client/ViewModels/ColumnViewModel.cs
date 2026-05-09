using System.Collections.ObjectModel;
using TaskBoard.Shared;

namespace TaskBoard.Client.ViewModels;

public sealed class ColumnViewModel(BoardColumn column, IEnumerable<TaskCard> cards)
{
    public BoardColumn Column { get; } = column;
    public Guid Id => Column.Id;
    public string Title => Column.Title;
    public ObservableCollection<CardViewModel> Cards { get; } = new(cards.Select(card => new CardViewModel(card)));
}
