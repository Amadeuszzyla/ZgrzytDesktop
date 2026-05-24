using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Services;

public class ApiConfigurationTests
{
    public ApiConfigurationTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void DefaultApiUrl_IsFixedRenderBackend()
    {
        Assert.Equal("https://zgrzyt-api.onrender.com/api/", ApiDefaults.ProductionApiBaseUrl);
        Assert.StartsWith("https://", ApiDefaults.ProductionApiBaseUrl, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("/api/", ApiDefaults.ProductionApiBaseUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiUrlNormalization_AppendsApi_WhenRootRenderUrlGiven()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);
            var result = service.NormalizeApiBaseUrl("https://zgrzyt-api.onrender.com/");

            Assert.Equal("https://zgrzyt-api.onrender.com/api/", result);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void ApiUrlNormalization_DoesNotDuplicateApiSegment()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);
            var result = service.NormalizeApiBaseUrl("https://zgrzyt-api.onrender.com/api/");

            Assert.Equal("https://zgrzyt-api.onrender.com/api/", result);
            Assert.DoesNotContain("api/api/", result, StringComparison.OrdinalIgnoreCase);

            var duplicatedInput = service.NormalizeApiBaseUrl("https://zgrzyt-api.onrender.com/api/api/");
            Assert.Equal("https://zgrzyt-api.onrender.com/api/", duplicatedInput);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void ApiUrlValidation_AllowsHttpsFixedRenderUrl()
    {
        Assert.Null(ApiUrlValidator.Validate(ApiDefaults.ProductionApiBaseUrl));
        Assert.Null(ApiUrlValidator.Validate("https://zgrzyt-api.onrender.com/api/"));
    }

    [Fact]
    public async Task LoadAsync_MigratesLegacyStolenApiHost_ToProduction()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);
            const string legacyUrl = "https://zgrzyt-stolen-api.onrender.com/api/";

            await service.SaveAsync(new AppSettings { ApiBaseUrl = legacyUrl });

            var loaded = await service.LoadAsync();

            Assert.Equal(ApiDefaults.ProductionApiBaseUrl, loaded.ApiBaseUrl);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Theory]
    [InlineData("https://zgrzyt-stolen-api.onrender.com/api/")]
    [InlineData("https://zgrzyt-stolen-api.onrender.com/")]
    public void NormalizeApiBaseUrl_MigratesLegacyStolenApiHost(string legacyUrl)
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);
            Assert.Equal(ApiDefaults.ProductionApiBaseUrl, service.NormalizeApiBaseUrl(legacyUrl));
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "ZgrzytDesktopTests", Guid.NewGuid().ToString("N"));

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
