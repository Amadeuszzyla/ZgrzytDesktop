using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using ZgrzytDesktop.Converters;

namespace ZgrzytDesktop.Views.DashboardParts;

public partial class TicketStatusBadgeView : UserControl
{
    private static readonly TicketStatusBadgeClassConverter ClassConverter = new();
    private readonly Border _badgeRoot;
    private readonly TextBlock _badgeText;

    public static readonly StyledProperty<string?> DisplayStatusProperty =
        AvaloniaProperty.Register<TicketStatusBadgeView, string?>(nameof(DisplayStatus));

    public TicketStatusBadgeView()
    {
        InitializeComponent();
        _badgeRoot = this.FindControl<Border>("BadgeRoot")!;
        _badgeText = this.FindControl<TextBlock>("BadgeText")!;
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == DisplayStatusProperty)
                UpdateBadge(DisplayStatus);
        };
        UpdateBadge(DisplayStatus);
    }

    public string? DisplayStatus
    {
        get => GetValue(DisplayStatusProperty);
        set => SetValue(DisplayStatusProperty, value);
    }

    private void UpdateBadge(string? status)
    {
        _badgeText.Text = status ?? string.Empty;
        ApplyClasses(
            ClassConverter.Convert(status, typeof(string), null, CultureInfo.CurrentCulture) as string);
    }

    private void ApplyClasses(string? classString)
    {
        _badgeRoot.Classes.Clear();
        if (string.IsNullOrWhiteSpace(classString))
            return;

        foreach (var className in classString.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            _badgeRoot.Classes.Add(className);
    }
}
