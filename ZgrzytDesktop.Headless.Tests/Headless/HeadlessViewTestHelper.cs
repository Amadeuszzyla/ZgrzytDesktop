using Avalonia.Controls;
using Avalonia.VisualTree;
using ZgrzytDesktop.Views;
using AvaloniaControl = Avalonia.Controls.Control;

namespace ZgrzytDesktop.Headless.Tests.Headless;

internal static class HeadlessViewTestHelper
{
    public static Window ShowInWindow(AvaloniaControl content, double width = 1280, double height = 900)
    {
        var window = new Window
        {
            Content = content,
            Width = width,
            Height = height
        };

        window.Show();
        return window;
    }

    public static LoginView CreateLoginView(object? dataContext = null)
    {
        var view = new LoginView();
        if (dataContext is not null)
            view.DataContext = dataContext;

        return view;
    }

    public static DashboardView CreateDashboardView(object dataContext)
    {
        var view = new DashboardView { DataContext = dataContext };
        return view;
    }

    public static IEnumerable<AvaloniaControl> EnumerateDescendants(AvaloniaControl root)
    {
        foreach (var child in root.GetVisualChildren().OfType<AvaloniaControl>())
        {
            yield return child;

            foreach (var descendant in EnumerateDescendants(child))
                yield return descendant;
        }
    }

    public static IEnumerable<T> FindDescendants<T>(AvaloniaControl root) where T : AvaloniaControl =>
        EnumerateDescendants(root).OfType<T>();

    public static bool ContainsText(AvaloniaControl root, string text)
    {
        foreach (var textBlock in FindDescendants<TextBlock>(root))
        {
            if (string.Equals(textBlock.Text, text, StringComparison.Ordinal) ||
                (textBlock.Text?.Contains(text, StringComparison.Ordinal) ?? false))
            {
                return true;
            }
        }

        foreach (var button in FindDescendants<Avalonia.Controls.Button>(root))
        {
            if (button.Content is string content &&
                (string.Equals(content, text, StringComparison.Ordinal) ||
                 content.Contains(text, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    public static int CountDescendants<T>(AvaloniaControl root) where T : AvaloniaControl =>
        FindDescendants<T>(root).Count();

    public static int CountDescendantsByTypeName(AvaloniaControl root, string typeName) =>
        EnumerateDescendants(root).Count(control => control.GetType().Name == typeName);

    public static TextBlock? FindTextBlockWithExactText(AvaloniaControl root, string text) =>
        FindDescendants<TextBlock>(root)
            .FirstOrDefault(textBlock => string.Equals(textBlock.Text, text, StringComparison.Ordinal));
}
