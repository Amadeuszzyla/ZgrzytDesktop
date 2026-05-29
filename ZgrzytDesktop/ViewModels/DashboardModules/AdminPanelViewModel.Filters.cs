using System.Collections.ObjectModel;
using System.Linq;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class AdminPanelViewModel
{
    private AdminListFilterOption? _selectedAdminUserFilter;
    private bool _suppressAdminFilterReload;

    public ObservableCollection<AdminListFilterOption> AdminUserFilterOptions { get; } = new();

    public AdminListFilterOption? SelectedAdminUserFilter
    {
        get => _selectedAdminUserFilter;
        set
        {
            if (!SetProperty(ref _selectedAdminUserFilter, value))
                return;

            if (!_suppressAdminFilterReload && _callbacks.GetIsAdminRole())
                SafeFireAndForget.Run(LoadUsersAsync());
        }
    }

    private void InitializeAdminUserFilters()
    {
        AdminUserFilterOptions.Clear();
        foreach (var option in AdminListFilterOption.All)
            AdminUserFilterOptions.Add(option);

        _suppressAdminFilterReload = true;
        SelectedAdminUserFilter = AdminUserFilterOptions.First(option => option.Filter == UserAdminListFilter.All);
        _suppressAdminFilterReload = false;
    }

    public void ApplyDefaultFilter() => SetAdminUserFilter(UserAdminListFilter.All, reload: false);

    private void NotifyAdminUserFilterLocalization()
    {
        var selectedFilter = SelectedAdminUserFilter?.Filter ?? UserAdminListFilter.All;

        AdminUserFilterOptions.Clear();
        foreach (var option in AdminListFilterOption.All)
            AdminUserFilterOptions.Add(option);

        SetAdminUserFilter(selectedFilter, reload: false);
    }

    private void SetAdminUserFilter(UserAdminListFilter filter, bool reload)
    {
        _suppressAdminFilterReload = true;
        SelectedAdminUserFilter =
            AdminUserFilterOptions.FirstOrDefault(option => option.Filter == filter) ??
            AdminUserFilterOptions.FirstOrDefault();
        _suppressAdminFilterReload = false;

        if (reload && _callbacks.GetIsAdminRole())
            SafeFireAndForget.Run(LoadUsersAsync());
    }

    private UserAdminListFilter GetSelectedListFilter() =>
        SelectedAdminUserFilter?.Filter ?? UserAdminListFilter.All;

    private string GetSelectedFilterLabel() =>
        SelectedAdminUserFilter?.Label ?? AppStrings.Get("Admin_Filter_All");
}
