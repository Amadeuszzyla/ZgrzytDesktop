using System;
using Avalonia;
using Avalonia.Controls;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Views.DashboardParts;

public partial class UserActiveBadgeView : UserControl
{
    private readonly Border _badgeRoot;
    private readonly TextBlock _badgeText;

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<UserActiveBadgeView, bool>(nameof(IsActive));

    public UserActiveBadgeView()
    {
        InitializeComponent();
        _badgeRoot = this.FindControl<Border>("BadgeRoot")!;
        _badgeText = this.FindControl<TextBlock>("BadgeText")!;
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == IsActiveProperty)
                UpdateBadge(IsActive);
        };
        UpdateBadge(IsActive);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private void UpdateBadge(bool isActive)
    {
        _badgeText.Text = isActive
            ? AppStrings.Get("Grid_Active")
            : AppStrings.Get("Admin_StatusInactive");

        _badgeRoot.Classes.Clear();
        _badgeRoot.Classes.Add("ticket-badge");
        _badgeRoot.Classes.Add(isActive ? "ticket-badge-status-closed" : "ticket-badge-status-default");
    }
}
