namespace ZgrzytDesktop.Models;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = ZgrzytDesktop.Constants.ApiDefaults.ProductionApiBaseUrl;

    public string ThemeMode { get; set; } = "Light";

    public string UiCulture { get; set; } = "pl";

    public bool AutoLogoutEnabled { get; set; } = true;

    public int AutoLogoutTimeoutMinutes { get; set; } = 30;
}