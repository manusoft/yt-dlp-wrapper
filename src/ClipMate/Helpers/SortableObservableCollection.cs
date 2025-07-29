namespace ClipMate.Helpers;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

public class SortableObservableCollection<T> : ObservableCollection<T>, IDisposable where T : INotifyPropertyChanged
{
    private readonly Func<T, int> _sortKeySelector;
    private readonly string _propertyToWatch;
    private bool _isDisposed;
    private CancellationTokenSource? _sortDebounceCts;

    public SortableObservableCollection(Func<T, int> sortKeySelector, string propertyToWatch)
    {
        _sortKeySelector = sortKeySelector;
        _propertyToWatch = propertyToWatch;

        CollectionChanged += OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (T item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        if (e.OldItems != null)
        {
            foreach (T item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyToWatch)
        {
            DebounceSort();
        }
    }

    private async void DebounceSort()
    {
        _sortDebounceCts?.Cancel();
        _sortDebounceCts = new();
        var token = _sortDebounceCts.Token;

        try
        {
            await Task.Delay(100, token);
            if (!token.IsCancellationRequested)
                Sort();
        }
        catch (TaskCanceledException) { }
    }

    public void Sort()
    {
        var sorted = this.OrderBy(_sortKeySelector).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            int currentIndex = IndexOf(sorted[i]);
            if (currentIndex != i && currentIndex != -1)
            {
                Move(currentIndex, i);
            }
        }
    }

    protected override void ClearItems()
    {
        foreach (var item in this)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
        base.ClearItems();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        CollectionChanged -= OnCollectionChanged;

        foreach (var item in this)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        _sortDebounceCts?.Cancel();
        _sortDebounceCts?.Dispose();
    }
}
