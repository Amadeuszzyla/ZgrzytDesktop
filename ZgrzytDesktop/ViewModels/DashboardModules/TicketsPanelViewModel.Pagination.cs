using System;
using System.Threading.Tasks;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketsPanelViewModel
{
    private bool _isChangingPageInternally;

    private int _currentPage = 1;
    private int _lastPage = 1;
    private int _pageSize = 10;
    private int _totalTickets;

    private int? _selectedPageNumber = 1;
    private int? _selectedPageSize = 10;

    public string PagePositionText => $"Strona {CurrentPage} z {LastPage}";

    public string PageTotalText => $"Razem: {TotalTickets}";

    public int? SelectedPageNumber
    {
        get => _selectedPageNumber;
        set
        {
            if (SetProperty(ref _selectedPageNumber, value))
            {
                if (value is null || value.Value < 1)
                    return;

                if (value.Value != CurrentPage)
                    CurrentPage = value.Value;
            }
        }
    }

    public int? SelectedPageSize
    {
        get => _selectedPageSize;
        set
        {
            if (SetProperty(ref _selectedPageSize, value))
            {
                if (value is null || value.Value <= 0)
                    return;

                if (value.Value != PageSize)
                    PageSize = value.Value;
            }
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (value < 1)
                value = 1;

            var maxPage = Math.Max(1, LastPage);

            if (value > maxPage)
                value = maxPage;

            if (SetProperty(ref _currentPage, value))
            {
                SetSelectedPageNumberSilently(value);
                RefreshPaginationProperties();

                if (!_isChangingPageInternally)
                    _ = LoadTicketsAsync();
            }
        }
    }

    public int LastPage
    {
        get => _lastPage;
        set
        {
            value = Math.Max(1, value);

            if (SetProperty(ref _lastPage, value))
            {
                if (CurrentPage > value)
                    SetCurrentPageSilently(value);

                UpdatePageNumbers();
                RefreshPaginationProperties();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value <= 0)
                value = 10;

            if (SetProperty(ref _pageSize, value))
            {
                SetSelectedPageSizeSilently(value);
                SetCurrentPageSilently(1);
                _ = LoadTicketsAsync();
            }
        }
    }

    public int TotalTickets
    {
        get => _totalTickets;
        set
        {
            if (SetProperty(ref _totalTickets, value))
            {
                OnPropertyChanged(nameof(PagePositionText));
                OnPropertyChanged(nameof(PageTotalText));
            }
        }
    }

    public bool IsOnLastPage => CurrentPage >= LastPage;

    public bool CanGoPreviousPage => CurrentPage > 1 && !IsLoading;

    public bool CanGoNextPage => CurrentPage < LastPage && !IsLoading;

    public bool CanRefreshTicketsNow => !IsLoading;

    internal void SetCurrentPageSilently(int page)
    {
        _isChangingPageInternally = true;
        CurrentPage = page;
        _isChangingPageInternally = false;

        SetSelectedPageNumberSilently(CurrentPage);
    }

    private async Task GoToFirstPageAsync()
    {
        if (CurrentPage == 1)
            return;

        CurrentPage = 1;
        await Task.CompletedTask;
    }

    private async Task GoToPreviousPageAsync()
    {
        if (CurrentPage <= 1)
            return;

        CurrentPage--;
        await Task.CompletedTask;
    }

    private async Task GoToNextPageAsync()
    {
        if (CurrentPage >= LastPage)
            return;

        CurrentPage++;
        await Task.CompletedTask;
    }

    private async Task GoToLastPageAsync()
    {
        if (CurrentPage == LastPage)
            return;

        CurrentPage = LastPage;
        await Task.CompletedTask;
    }

    private void SetSelectedPageNumberSilently(int? page)
    {
        if (_selectedPageNumber == page)
            return;

        _selectedPageNumber = page;
        OnPropertyChanged(nameof(SelectedPageNumber));
    }

    private void SetSelectedPageSizeSilently(int? pageSize)
    {
        if (_selectedPageSize == pageSize)
            return;

        _selectedPageSize = pageSize;
        OnPropertyChanged(nameof(SelectedPageSize));
    }

    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();

        var pageCount = Math.Max(1, LastPage);

        for (var i = 1; i <= pageCount; i++)
            PageNumbers.Add(i);

        if (PageNumbers.Contains(CurrentPage))
            SetSelectedPageNumberSilently(CurrentPage);
        else
            SetSelectedPageNumberSilently(null);

        RefreshPaginationProperties();
    }

    private void RefreshPaginationProperties()
    {
        OnPropertyChanged(nameof(PagePositionText));
        OnPropertyChanged(nameof(PageTotalText));
        OnPropertyChanged(nameof(IsOnLastPage));
        OnPropertyChanged(nameof(CanGoPreviousPage));
        OnPropertyChanged(nameof(CanGoNextPage));
        OnPropertyChanged(nameof(CanRefreshTicketsNow));
        _callbacks.RefreshPaginationSideEffects();
    }
}
