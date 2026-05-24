using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnUserActivity, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnUserActivity, handledEventsToo: true);
        AddHandler(KeyDownEvent, OnUserActivity, handledEventsToo: true);
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel &&
            ConfirmationServiceHolder.Instance is UserConfirmationService confirmationService)
        {
            confirmationService.SetDialogOwner(this);
        }
    }

    private void OnUserActivity(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.RecordUserActivity();
    }
}
