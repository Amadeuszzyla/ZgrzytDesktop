using Avalonia.Controls;
using Avalonia.Interactivity;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Views.DashboardParts;

public partial class TicketsPanelView : UserControl
{
    public TicketsPanelView()
    {
        InitializeComponent();
    }

    private void OnCreateUserToolbarClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm)
            return;

        if (vm.ShowAdminPageCommand.CanExecute(null))
            vm.ShowAdminPageCommand.Execute(null);

        if (vm.ShowAdminNewAccountTabCommand.CanExecute(null))
            vm.ShowAdminNewAccountTabCommand.Execute(null);
    }

    private void OnManageUsersToolbarClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm)
            return;

        if (vm.ShowAdminPageCommand.CanExecute(null))
            vm.ShowAdminPageCommand.Execute(null);

        if (vm.ShowAdminUsersTabCommand.CanExecute(null))
            vm.ShowAdminUsersTabCommand.Execute(null);
    }
}
