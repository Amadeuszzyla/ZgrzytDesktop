using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Tests.Services;

public class SettingsServiceTests
{
    [Theory]
    [InlineData("", ApiDefaults.ProductionApiBaseUrl)]
    [InlineData("127.0.0.1:9000", ApiDefaults.ProductionApiBaseUrl)]
    [InlineData("http://127.0.0.1:9000", ApiDefaults.ProductionApiBaseUrl)]
    [InlineData("http://localhost:9000/api/", ApiDefaults.ProductionApiBaseUrl)]
    [InlineData("https://zgrzyt-api.onrender.com", "https://zgrzyt-api.onrender.com/api/")]
    [InlineData("https://zgrzyt-api.onrender.com/", "https://zgrzyt-api.onrender.com/api/")]
    [InlineData("https://zgrzyt-api.onrender.com/api", "https://zgrzyt-api.onrender.com/api/")]
    [InlineData("https://zgrzyt-api.onrender.com/api/", "https://zgrzyt-api.onrender.com/api/")]
    public void NormalizeApiBaseUrl_ShouldReturnCorrectApiUrl(string input, string expected)
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);

            var result = service.NormalizeApiBaseUrl(input);

            Assert.Equal(expected, result);
            Assert.DoesNotContain("api/api/", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void NormalizeApiBaseUrl_CustomHost_ShouldNotMigrateToProduction()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);

            var result = service.NormalizeApiBaseUrl("https://staging.example.com");

            Assert.Equal("https://staging.example.com/api/", result);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ShouldReturnSavedSettings()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);

            var settings = new AppSettings
            {
                ApiBaseUrl = "https://zgrzyt-api.onrender.com"
            };

            await service.SaveAsync(settings);

            var loaded = await service.LoadAsync();

            Assert.Equal(ApiDefaults.ProductionApiBaseUrl, loaded.ApiBaseUrl);
            Assert.Equal("System", loaded.ThemeMode);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ShouldReturnDefaultSettings()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);

            var loaded = await service.LoadAsync();

            Assert.Equal(ApiDefaults.ProductionApiBaseUrl, loaded.ApiBaseUrl);
            Assert.Equal("System", loaded.ThemeMode);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LoadAsync_WithLegacyLocalhost_ShouldMigrateToProduction()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);
            var legacy = new AppSettings
            {
                ApiBaseUrl = "http://127.0.0.1:9000/api/",
                ThemeMode = "System"
            };

            await service.SaveAsync(legacy);

            var loaded = await service.LoadAsync();

            Assert.Equal(ApiDefaults.ProductionApiBaseUrl, loaded.ApiBaseUrl);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Theory]
    [InlineData("System", "System")]
    [InlineData("Light", "Light")]
    [InlineData("Dark", "Dark")]
    [InlineData("invalid", "System")]
    public void NormalizeThemeMode_ShouldReturnExpectedValue(string input, string expected)
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);

            Assert.Equal(expected, service.NormalizeThemeMode(input));
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task SaveAsync_WithThemeMode_ShouldPersistThemeMode()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);

            var settings = new AppSettings
            {
                ApiBaseUrl = ApiDefaults.ProductionApiBaseUrl,
                ThemeMode = "Dark"
            };

            await service.SaveAsync(settings);

            var loaded = await service.LoadAsync();

            Assert.Equal("Dark", loaded.ThemeMode);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static string CreateTempDirectory()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "ZgrzytDesktopTests",
            Guid.NewGuid().ToString("N")
        );
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
