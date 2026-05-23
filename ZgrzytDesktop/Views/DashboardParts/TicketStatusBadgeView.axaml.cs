using System;
using Avalonia;
using Avalonia.Controls;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Views.DashboardParts;

public partial class TicketStatusBadgeView : UserControl
{
    private readonly Border _badgeRoot;
    private readonly TextBlock _badgeText;

    public static readonly StyledProperty<string?> ApiStatusProperty =
        AvaloniaProperty.Register<TicketStatusBadgeView, string?>(nameof(ApiStatus));

    public TicketStatusBadgeView()
    {
        InitializeComponent();
        _badgeRoot = this.FindControl<Border>("BadgeRoot")!;
        _badgeText = this.FindControl<TextBlock>("BadgeText")!;
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == ApiStatusProperty)
                UpdateBadge(ApiStatus);
        };
        UpdateBadge(ApiStatus);
    }

    public string? ApiStatus
    {
        get => GetValue(ApiStatusProperty);
        set => SetValue(ApiStatusProperty, value);
    }

    public void RefreshDisplay() => UpdateBadge(ApiStatus);

    private void UpdateBadge(string? apiStatus)
    {
        _badgeText.Text = StatusDisplayHelper.ToDisplayStatus(apiStatus);
        ApplyClasses(StatusDisplayHelper.GetStatusBadgeClasses(apiStatus));
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
