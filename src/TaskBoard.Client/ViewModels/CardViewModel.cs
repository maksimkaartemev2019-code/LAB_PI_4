using CommunityToolkit.Mvvm.ComponentModel;
using TaskBoard.Shared;

namespace TaskBoard.Client.ViewModels;

public sealed class CardViewModel(TaskCard card) : ObservableObject
{
    public TaskCard Card { get; } = card;
    public Guid Id => Card.Id;
    public Guid ColumnId => Card.ColumnId;

    public string Title
    {
        get => Card.Title;
        set
        {
            if (Card.Title != value)
            {
                Card.Title = value;
                OnPropertyChanged();
            }
        }
    }

    public string Description
    {
        get => Card.Description;
        set
        {
            if (Card.Description != value)
            {
                Card.Description = value;
                OnPropertyChanged();
            }
        }
    }
}
