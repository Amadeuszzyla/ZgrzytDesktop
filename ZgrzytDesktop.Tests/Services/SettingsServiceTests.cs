using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Tests.Services;

public class SettingsServiceTests
{
    [Theory]
    [InlineData("", "http://127.0.0.1:9000/api/")]
    [InlineData("127.0.0.1:9000", "http://127.0.0.1:9000/api/")]
    [InlineData("http://127.0.0.1:9000", "http://127.0.0.1:9000/api/")]
    [InlineData("http://127.0.0.1:9000/", "http://127.0.0.1:9000/api/")]
    [InlineData("http://127.0.0.1:9000/api", "http://127.0.0.1:9000/api/")]
    [InlineData("http://127.0.0.1:9000/api/", "http://127.0.0.1:9000/api/")]
    public void NormalizeApiBaseUrl_ShouldReturnCorrectApiUrl(string input, string expected)
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);

            var result = service.NormalizeApiBaseUrl(input);

            Assert.Equal(expected, result);
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
                ApiBaseUrl = "127.0.0.1:9000"
            };

            await service.SaveAsync(settings);

            var loaded = await service.LoadAsync();

            Assert.Equal("http://127.0.0.1:9000/api/", loaded.ApiBaseUrl);
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

            Assert.Equal("http://127.0.0.1:9000/api/", loaded.ApiBaseUrl);
            Assert.Equal("System", loaded.ThemeMode);
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
                ApiBaseUrl = "http://127.0.0.1:9000/api/",
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