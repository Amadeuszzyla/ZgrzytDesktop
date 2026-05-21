namespace ZgrzytDesktop.Models;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = "http://127.0.0.1:9000/api/";

    public string ThemeMode { get; set; } = "System";

    public string UiCulture { get; set; } = "pl";
}