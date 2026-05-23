using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Views.DashboardParts;

public partial class AdminPanelView : UserControl
{
    public AdminPanelView()
    {
        InitializeComponent();
    }

    private void OnActivateUserClick(object? sender, RoutedEventArgs e) =>
        ExecuteUserAction(sender, vm => vm.ActivateAdminUserCommand);

    private void OnBanUserClick(object? sender, RoutedEventArgs e) =>
        ExecuteUserAction(sender, vm => vm.BanAdminUserCommand);

    private void OnUnbanUserClick(object? sender, RoutedEventArgs e) =>
        ExecuteUserAction(sender, vm => vm.UnbanAdminUserCommand);

    private void ExecuteUserAction(object? sender, Func<DashboardViewModel, IAsyncRelayCommand> getCommand)
    {
        if (sender is not Button { DataContext: User user } || DataContext is not DashboardViewModel vm)
            return;

        vm.SelectedAdminUser = user;

        var command = getCommand(vm);
        if (command.CanExecute(null))
            command.Execute(null);
    }
}
