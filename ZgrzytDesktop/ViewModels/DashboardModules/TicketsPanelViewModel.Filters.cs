using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketsPanelViewModel
{
    private string _searchText = string.Empty;

    private TicketSortFieldOption? _selectedTicketSortField;
    private TicketSortDirectionOption? _selectedTicketSortDirection;

    private string _selectedFilterStatus = FilterLabels.All;
    private string _selectedFilterPriority = FilterLabels.All;
    private string _selectedAssignmentFilterKey = TicketAssignmentFilterKeys.All;
    private string _selectedTicketQueueView = FilterLabels.All;
    private string _selectedCategoryFilterKey = TicketCategoryFilterKeys.All;

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
            if (value is null || _selectedFilterStatus == value.Value)
                return;

            SelectedFilterStatus = value.Value;
            SetCurrentPageSilently(1);
            _ = LoadTicketsAsync();
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
            if (value is null || _selectedFilterPriority == value.Value)
                return;

            SelectedFilterPriority = value.Value;
            SetCurrentPageSilently(1);
            _ = LoadTicketsAsync();
        }
    }

    public string SelectedAssignmentFilterKey
    {
        get => _selectedAssignmentFilterKey;
        set => SetProperty(ref _selectedAssignmentFilterKey, value);
    }

    public TicketFilterOption? SelectedFilterAssignmentOption
    {
        get => FilterAssignmentOptions.FirstOrDefault(option => option.Value == _selectedAssignmentFilterKey);
        set
        {
            if (value is null || _selectedAssignmentFilterKey == value.Value)
                return;

            _selectedAssignmentFilterKey = value.Value;
            OnPropertyChanged(nameof(SelectedAssignmentFilterKey));
            OnPropertyChanged(nameof(SelectedFilterAssignmentOption));
            SetCurrentPageSilently(1);
            _ = LoadTicketsAsync();
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

    public string LblFilterCategory => AppStrings.Get("Tickets_FilterCategory");

    public string SelectedCategory
    {
        get => SelectedCategoryFilterKey;
        set
        {
            var option = FilterCategoryOptions.FirstOrDefault(o =>
                string.Equals(o.Key, value, StringComparison.OrdinalIgnoreCase));

            if (option is not null)
                SelectedCategoryFilterOption = option;
        }
    }

    public string SelectedCategoryFilterKey
    {
        get => _selectedCategoryFilterKey;
        set => SetProperty(ref _selectedCategoryFilterKey, value);
    }

    public TicketCategoryFilterOption? SelectedCategoryFilterOption
    {
        get => FilterCategoryOptions.FirstOrDefault(option =>
            string.Equals(option.Key, _selectedCategoryFilterKey, StringComparison.OrdinalIgnoreCase));
        set
        {
            if (value is null)
                return;

            if (_selectedCategoryFilterKey == value.Key)
                return;

            _selectedCategoryFilterKey = value.Key;
            OnPropertyChanged(nameof(SelectedCategoryFilterKey));
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(SelectedCategoryFilterOption));
            SetCurrentPageSilently(1);
            _ = LoadTicketsAsync();
        }
    }

    public IReadOnlyList<string> AvailableCategories => TicketCategoryFilterKeys.AllKeys;

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
        _selectedAssignmentFilterKey = TicketAssignmentFilterKeys.All;
        OnPropertyChanged(nameof(SelectedAssignmentFilterKey));
        OnPropertyChanged(nameof(SelectedFilterAssignmentOption));
        SetSelectedTicketQueueViewSilently(FilterLabels.All);
        _selectedCategoryFilterKey = TicketCategoryFilterKeys.All;
        OnPropertyChanged(nameof(SelectedCategoryFilterKey));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(SelectedCategoryFilterOption));
        SetCurrentPageSilently(1);

        _ = LoadTicketsAsync();
    }

    private void InitializeCollections()
    {
        foreach (var size in new[] { 5, 10, 20, 50 })
            PageSizeOptions.Add(size);

        RefreshFilterCollections();
        RefreshFilterCategoryOptions();

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
        var filterAssignment = _selectedAssignmentFilterKey;

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

        FilterAssignmentOptions.Clear();
        foreach (var key in TicketAssignmentFilterKeys.AllKeys)
            FilterAssignmentOptions.Add(new TicketFilterOption(key, TicketFilterOptionKind.Assignment));

        _selectedFilterStatus = filterStatus;
        _selectedFilterPriority = filterPriority;
        _selectedAssignmentFilterKey = filterAssignment;

        OnPropertyChanged(nameof(SelectedFilterStatus));
        OnPropertyChanged(nameof(SelectedFilterStatusOption));
        OnPropertyChanged(nameof(SelectedFilterPriority));
        OnPropertyChanged(nameof(SelectedFilterPriorityOption));
        OnPropertyChanged(nameof(SelectedAssignmentFilterKey));
        OnPropertyChanged(nameof(SelectedFilterAssignmentOption));
    }

    internal void RefreshFilterCategoryOptions()
    {
        var selectedKey = _selectedCategoryFilterKey;

        FilterCategoryOptions.Clear();

        foreach (var key in TicketCategoryFilterKeys.AllKeys)
            FilterCategoryOptions.Add(new TicketCategoryFilterOption(key));

        _selectedCategoryFilterKey = FilterCategoryOptions
            .FirstOrDefault(option => string.Equals(option.Key, selectedKey, StringComparison.OrdinalIgnoreCase))
            ?.Key ?? TicketCategoryFilterKeys.All;

        OnPropertyChanged(nameof(FilterCategoryOptions));
        OnPropertyChanged(nameof(SelectedCategoryFilterKey));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(SelectedCategoryFilterOption));
        OnPropertyChanged(nameof(LblFilterCategory));
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
