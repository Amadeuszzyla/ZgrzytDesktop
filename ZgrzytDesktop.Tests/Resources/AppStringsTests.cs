using System.Globalization;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Resources;

public class AppStringsTests
{
    public static TheoryData<string, string, string> MainUiKeys => new()
    {
        { "Nav_Tickets", "Zgłoszenia", "Tickets" },
        { "Nav_Statistics", "Statystyki", "Statistics" },
        { "Nav_Settings", "Ustawienia", "Settings" },
        { "Nav_Admin", "Administracja", "Administration" },
        { "Settings_Title", "Ustawienia aplikacji", "Application settings" },
        { "Stats_Title", "Statystyki zgłoszeń", "Ticket statistics" },
        { "Api_Unauthorized", "Sesja wygasła albo użytkownik nie jest zalogowany.", "Session expired or user is not logged in." },
        { "Api_Forbidden", "Brak uprawnień do wykonania tej operacji.", "You do not have permission to perform this action." },
        { "Toast_SettingsSaved", "Ustawienia zapisane", "Settings saved" },
        { "Toast_UserBanned", "Użytkownik został zbanowany.", "User has been banned." }
    };

    [Theory]
    [MemberData(nameof(MainUiKeys))]
    public void Get_HasPolishAndEnglishValues(string key, string polish, string english)
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal(polish, AppStrings.Get(key));

        AppStrings.ApplyCulture("en");
        Assert.Equal(english, AppStrings.Get(key));
    }

    [Fact]
    public void Get_WithMissingKey_ReturnsKeyWithoutThrowing()
    {
        AppStrings.ApplyCulture("pl");

        var value = AppStrings.Get("Key_That_Does_Not_Exist_12345");

        Assert.Equal("Key_That_Does_Not_Exist_12345", value);
    }

    [Fact]
    public void ApplyCulture_NormalizesEnAndPlVariants()
    {
        AppStrings.ApplyCulture("en-US");
        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);

        AppStrings.ApplyCulture("pl");
        Assert.Equal("pl-PL", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task SaveAsync_PersistsUiCulture()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();

        try
        {
            var settingsService = new SettingsService(directory);
            var settings = new AppSettings
            {
                ApiBaseUrl = "http://127.0.0.1:9000/api/",
                ThemeMode = "System",
                UiCulture = "en"
            };

            await settingsService.SaveAsync(settings);

            var loaded = await settingsService.LoadAsync();

            Assert.Equal("en", loaded.UiCulture);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }
}
