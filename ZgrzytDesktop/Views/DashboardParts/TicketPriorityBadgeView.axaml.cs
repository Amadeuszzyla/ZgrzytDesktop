using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using ZgrzytDesktop.Converters;

namespace ZgrzytDesktop.Views.DashboardParts;

public partial class TicketPriorityBadgeView : UserControl
{
    private static readonly TicketPriorityBadgeClassConverter ClassConverter = new();
    private readonly Border _badgeRoot;
    private readonly TextBlock _badgeText;

    public static readonly StyledProperty<string?> PriorityProperty =
        AvaloniaProperty.Register<TicketPriorityBadgeView, string?>(nameof(Priority));

    public TicketPriorityBadgeView()
    {
        InitializeComponent();
        _badgeRoot = this.FindControl<Border>("BadgeRoot")!;
        _badgeText = this.FindControl<TextBlock>("BadgeText")!;
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == PriorityProperty)
                UpdateBadge(Priority);
        };
        UpdateBadge(Priority);
    }

    public string? Priority
    {
        get => GetValue(PriorityProperty);
        set => SetValue(PriorityProperty, value);
    }

    private void UpdateBadge(string? priority)
    {
        _badgeText.Text = priority ?? string.Empty;
        ApplyClasses(
            ClassConverter.Convert(priority, typeof(string), null, CultureInfo.CurrentCulture) as string);
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
