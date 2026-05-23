using Avalonia;
using Avalonia.Controls;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Views.DashboardParts;

public partial class UserBanBadgeView : UserControl
{
    public static readonly StyledProperty<bool> IsBannedProperty =
        AvaloniaProperty.Register<UserBanBadgeView, bool>(nameof(IsBanned));

    public UserBanBadgeView()
    {
        InitializeComponent();
        var badgeText = this.FindControl<TextBlock>("BadgeText")!;
        badgeText.Text = AppStrings.Get("Grid_Ban");
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == IsBannedProperty)
                IsVisible = IsBanned;
        };
        IsVisible = IsBanned;
    }

    public bool IsBanned
    {
        get => GetValue(IsBannedProperty);
        set => SetValue(IsBannedProperty, value);
    }
}
