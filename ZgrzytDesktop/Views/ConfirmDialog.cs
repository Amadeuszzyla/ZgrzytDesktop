using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Views;

public static class ConfirmDialog
{
    public static async Task<bool> ShowAsync(Window owner, string title, string message)
    {
        var result = false;
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            MinHeight = 180,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        var confirmButton = new Button
        {
            Content = AppStrings.Get("Confirm_Yes"),
            MinWidth = 100,
            MinHeight = 36
        };

        var cancelButton = new Button
        {
            Content = AppStrings.Get("Confirm_No"),
            MinWidth = 100,
            MinHeight = 36
        };

        confirmButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        dialog.Content = new Border
        {
            Padding = new Thickness(24),
            Child = new StackPanel
            {
                Spacing = 20,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.Black
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancelButton, confirmButton }
                    }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
