using System;
using System.Linq;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketsPanelViewModel
{
    private string _searchText = string.Empty;

    private TicketSortFieldOption? _selectedTicketSortField;
    private TicketSortDirectionOption? _selectedTicketSortDirection;

    private string _selectedFilterStatus = FilterLabels.All;
    private string _selectedFilterPriority = FilterLabels.All;
    private string _selectedTicketQueueView = FilterLabels.All;

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public TicketSortFieldOption? SelectedTicketSortField
    {
        get => _selectedTicketSortField;
        set
        {
            if (SetProperty(ref _selectedTicketSortField, value))
            {
                SetCurrentPageSilently(1);
                _ = LoadTicketsAsync();
            }
        }
    }

    public TicketSortDirectionOption? SelectedTicketSortDirection
    {
        get => _selectedTicketSortDirection;
        set
        {
            if (SetProperty(ref _selectedTicketSortDirection, value))
            {
                SetCurrentPageSilently(1);
                _ = LoadTicketsAsync();
            }
        }
    }

    public string SelectedFilterStatus
    {
        get => _selectedFilterStatus;
        set => SetProperty(ref _selectedFilterStatus, value);
    }

    public TicketFilterOption? SelectedFilterStatusOption
    {
        get => FilterStatusOptions.FirstOrDefault(option => option.Value == _selectedFilterStatus);
        set
        {
            if (value is null)
                return;

            SelectedFilterStatus = value.Value;
        }
    }

    public string SelectedFilterPriority
    {
        get => _selectedFilterPriority;
        set => SetProperty(ref _selectedFilterPriority, value);
    }

    public TicketFilterOption? SelectedFilterPriorityOption
    {
        get => FilterPriorityOptions.FirstOrDefault(option => option.Value == _selectedFilterPriority);
        set
        {
            if (value is null)
                return;

            SelectedFilterPriority = value.Value;
        }
    }

    public string SelectedTicketQueueView
    {
        get => _selectedTicketQueueView;
        set
        {
            if (SetProperty(ref _selectedTicketQueueView, value))
            {
                SetCurrentPageSilently(1);
                _ = LoadTicketsAsync();
            }
        }
    }

    public TicketFilterOption? SelectedTicketQueueViewOption
    {
        get => TicketQueueViewOptions.FirstOrDefault(option => option.Value == _selectedTicketQueueView);
        set
        {
            if (value is null)
                return;

            SetSelectedTicketQueueViewSilently(value.Value);
            SetCurrentPageSilently(1);
            _ = LoadTicketsAsync();
        }
    }

    private async Task SearchTicketsAsync()
    {
        SetCurrentPageSilently(1);
        await LoadTicketsAsync();
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedFilterStatus = FilterLabels.All;
        SelectedFilterPriority = FilterLabels.All;
        SetSelectedTicketQueueViewSilently(FilterLabels.All);
        SetCurrentPageSilently(1);

        _ = LoadTicketsAsync();
    }

    private void InitializeCollections()
    {
        foreach (var size in new[] { 5, 10, 20, 50 })
            PageSizeOptions.Add(size);

        RefreshFilterCollections();

        foreach (var field in TicketSortHelper.Fields)
            TicketSortFields.Add(field);

        foreach (var direction in TicketSortHelper.Directions)
            TicketSortDirections.Add(direction);

        RefreshCategoryOptions();
    }

    internal void RefreshFilterCollections()
    {
        var filterStatus = _selectedFilterStatus;
        var filterPriority = _selectedFilterPriority;

        FilterStatusOptions.Clear();
        foreach (var value in new[]
                 {
                     FilterLabels.All,
                     TicketStatuses.Nowe,
                     TicketStatuses.WTrakcie,
                     TicketStatuses.Zamkniete
                 })
            FilterStatusOptions.Add(new TicketFilterOption(value, TicketFilterOptionKind.Status));

        FilterPriorityOptions.Clear();
        foreach (var value in new[]
                 {
                     FilterLabels.All,
                     TicketPriorities.Low,
                     TicketPriorities.Medium,
                     TicketPriorities.High
                 })
            FilterPriorityOptions.Add(new TicketFilterOption(value, TicketFilterOptionKind.Priority));

        _selectedFilterStatus = filterStatus;
        _selectedFilterPriority = filterPriority;

        OnPropertyChanged(nameof(SelectedFilterStatus));
        OnPropertyChanged(nameof(SelectedFilterStatusOption));
        OnPropertyChanged(nameof(SelectedFilterPriority));
        OnPropertyChanged(nameof(SelectedFilterPriorityOption));
    }

    internal void RefreshCategoryOptions()
    {
        var selectedCategory = _selectedNewTicketCategory;

        NewTicketCategories.Clear();

        foreach (var category in TicketCategoryHelper.Categories)
            NewTicketCategories.Add(category);

        if (NewTicketCategories.Contains(selectedCategory))
            SelectedNewTicketCategory = selectedCategory;
        else if (NewTicketCategories.Count > 0)
            SelectedNewTicketCategory = NewTicketCategories[0];
    }

    private void SetSelectedTicketQueueViewSilently(string value)
    {
        if (_selectedTicketQueueView == value)
            return;

        _selectedTicketQueueView = value;
        OnPropertyChanged(nameof(SelectedTicketQueueView));
        OnPropertyChanged(nameof(SelectedTicketQueueViewOption));
    }

    private static TicketQueueView GetSelectedTicketQueueView(string selectedTicketQueueView) =>
        selectedTicketQueueView switch
        {
            FilterLabels.Active => TicketQueueView.Active,
            FilterLabels.Unassigned => TicketQueueView.Unassigned,
            _ => TicketQueueView.All
        };

    private TicketQueueView GetSelectedTicketQueueView() =>
        GetSelectedTicketQueueView(SelectedTicketQueueView);

    private static string? GetSelectedFilterValue(string value) =>
        FilterLabels.IsAll(value)
            ? null
            : value;
}
