using System;
using Avalonia;
using Avalonia.Controls;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Views.DashboardParts;

public partial class TicketPriorityBadgeView : UserControl
{
    private readonly Border _badgeRoot;
    private readonly TextBlock _badgeText;

    public static readonly StyledProperty<string?> ApiPriorityProperty =
        AvaloniaProperty.Register<TicketPriorityBadgeView, string?>(nameof(ApiPriority));

    public TicketPriorityBadgeView()
    {
        InitializeComponent();
        _badgeRoot = this.FindControl<Border>("BadgeRoot")!;
        _badgeText = this.FindControl<TextBlock>("BadgeText")!;
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == ApiPriorityProperty)
                UpdateBadge(ApiPriority);
        };
        UpdateBadge(ApiPriority);
    }

    public string? ApiPriority
    {
        get => GetValue(ApiPriorityProperty);
        set => SetValue(ApiPriorityProperty, value);
    }

    public void RefreshDisplay() => UpdateBadge(ApiPriority);

    private void UpdateBadge(string? apiPriority)
    {
        _badgeText.Text = PriorityDisplayHelper.ToDisplayPriority(apiPriority);
        ApplyClasses(PriorityDisplayHelper.GetPriorityBadgeClasses(apiPriority));
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
