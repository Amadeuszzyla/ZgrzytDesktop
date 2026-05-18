using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Tests.Cache;

public class LocalTicketCacheServiceTests
{
    [Fact]
    public async Task SaveTicketsAsync_ThenLoadTicketsAsync_ShouldReturnSavedTickets()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new LocalTicketCacheService(directory);

            var tickets = new List<Ticket>
            {
                new()
                {
                    Id = 1,
                    Title = "Problem z komputerem",
                    Description = "Komputer nie uruchamia się.",
                    Status = "nowe",
                    Priority = "wysoki"
                },
                new()
                {
                    Id = 2,
                    Title = "Problem z drukarką",
                    Description = "Drukarka nie drukuje.",
                    Status = "w trakcie",
                    Priority = "średni"
                }
            };

            await service.SaveTicketsAsync(tickets);

            var loaded = await service.LoadTicketsAsync();

            Assert.Equal(2, loaded.Count);

            Assert.Equal(1, loaded[0].Id);
            Assert.Equal("Problem z komputerem", loaded[0].Title);
            Assert.Equal("nowe", loaded[0].Status);
            Assert.Equal("wysoki", loaded[0].Priority);

            Assert.Equal(2, loaded[1].Id);
            Assert.Equal("Problem z drukarką", loaded[1].Title);
            Assert.Equal("w trakcie", loaded[1].Status);
            Assert.Equal("średni", loaded[1].Priority);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LoadTicketsAsync_WhenFileDoesNotExist_ShouldReturnEmptyList()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new LocalTicketCacheService(directory);

            var loaded = await service.LoadTicketsAsync();

            Assert.NotNull(loaded);
            Assert.Empty(loaded);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveSavedTickets()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new LocalTicketCacheService(directory);

            var tickets = new List<Ticket>
            {
                new()
                {
                    Id = 1,
                    Title = "Test",
                    Description = "Opis testowy",
                    Status = "nowe",
                    Priority = "niski"
                }
            };

            await service.SaveTicketsAsync(tickets);
            await service.ClearAsync();

            var loaded = await service.LoadTicketsAsync();

            Assert.Empty(loaded);
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