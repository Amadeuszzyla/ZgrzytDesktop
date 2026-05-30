using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Views;
using AvaloniaControl = Avalonia.Controls.Control;

namespace ZgrzytDesktop.Headless.Tests.Headless;

internal static class HeadlessViewTestHelper
{
    private static readonly List<Window> OpenWindows = [];

    public static Window ShowInWindow(AvaloniaControl content, double width = 1280, double height = 900)
    {
        var window = new Window
        {
            Content = content,
            Width = width,
            Height = height
        };

        window.Show();
        OpenWindows.Add(window);
        WaitForUiIdle(content);
        return window;
    }

    public static void CloseWindow(Window? window)
    {
        if (window is null)
            return;

        OpenWindows.Remove(window);
        window.Close();
        WaitForUiIdle();
    }

    public static void CloseAllOpenWindows()
    {
        while (OpenWindows.Count > 0)
            CloseWindow(OpenWindows[^1]);
    }

    public static void ResetSharedTestState()
    {
        CloseAllOpenWindows();
        ApplyUiCulture("pl");
        WaitForUiIdle();
    }

    public static void ApplyUiCulture(string culture)
    {
        AppStrings.ApplyCulture(culture);
    }

    public static void WaitForUiIdle(AvaloniaControl? root = null)
    {
        for (var pass = 0; pass < 3; pass++)
        {
            if (root is not null)
                root.UpdateLayout();

            Dispatcher.UIThread.RunJobs();
        }
    }

    public static void RefreshDataContext(AvaloniaControl control, object? dataContext)
    {
        control.DataContext = null;
        WaitForUiIdle(control);
        control.DataContext = dataContext;
        WaitForUiIdle(control);
    }

    public static void WaitForCondition(Func<bool> predicate, int timeoutMs = 5000, int pollIntervalMs = 10)
    {
        var deadline = Environment.TickCount64 + timeoutMs;

        while (!predicate())
        {
            if (Environment.TickCount64 >= deadline)
                throw new TimeoutException("UI condition was not met before timeout.");

            WaitForUiIdle();
            Thread.Sleep(pollIntervalMs);
        }

        WaitForUiIdle();
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

    public static Avalonia.Controls.ComboBox? FindCategoryFilterComboBox(AvaloniaControl root) =>
        FindDescendants<Avalonia.Controls.ComboBox>(root)
            .FirstOrDefault(comboBox =>
                comboBox.ItemsSource is IEnumerable<object> items &&
                items.Any(item => item is TicketCategoryFilterOption));

    public static bool ComboBoxHasSelectedCategory(AvaloniaControl root, string categoryKey, string expectedLabel)
    {
        var comboBox = FindCategoryFilterComboBox(root);
        if (comboBox?.SelectedItem is not TicketCategoryFilterOption selected)
            return false;

        return string.Equals(selected.Key, categoryKey, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(selected.Label, expectedLabel, StringComparison.Ordinal);
    }

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
