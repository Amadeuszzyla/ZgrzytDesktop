namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    internal sealed class BootstrapOptions
    {
        public bool EnableTimers { get; init; } = true;

        public bool RunInitialLoad { get; init; } = true;

        public bool ShowLoginToast { get; init; } = true;

        public static BootstrapOptions Production { get; } = new();

        public static BootstrapOptions Testing { get; } = new()
        {
            EnableTimers = false,
            RunInitialLoad = false,
            ShowLoginToast = false
        };

        public static BootstrapOptions TestingWithTimers { get; } = new()
        {
            EnableTimers = true,
            RunInitialLoad = false,
            ShowLoginToast = false
        };
    }
}
