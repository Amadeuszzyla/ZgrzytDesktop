using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Tests.Cache;

public class LocalUserCacheServiceTests
{
    [Fact]
    public async Task SaveUserAsync_ThenLoadUserAsync_ShouldReturnSavedUser()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new LocalUserCacheService(directory);

            var user = new User
            {
                Id = 1,
                Name = "Administrator",
                Role = "admin"
            };

            await service.SaveUserAsync(user);

            var loaded = await service.LoadUserAsync();

            Assert.NotNull(loaded);
            Assert.Equal(1, loaded.Id);
            Assert.Equal("Administrator", loaded.Name);
            Assert.Equal("admin", loaded.Role);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LoadUserAsync_WhenFileDoesNotExist_ShouldReturnNull()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new LocalUserCacheService(directory);

            var loaded = await service.LoadUserAsync();

            Assert.Null(loaded);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveSavedUser()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new LocalUserCacheService(directory);

            var user = new User
            {
                Id = 2,
                Name = "User",
                Role = "user"
            };

            await service.SaveUserAsync(user);
            await service.ClearAsync();

            var loaded = await service.LoadUserAsync();

            Assert.Null(loaded);
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