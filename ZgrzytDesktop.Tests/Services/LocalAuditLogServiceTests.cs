using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Services;

public class LocalAuditLogServiceTests
{
    [Fact]
    public async Task AddAsync_SavesEntry()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();

        try
        {
            var service = new LocalAuditLogService(directory);
            var entry = new AuditLogEntry
            {
                Timestamp = new DateTime(2026, 5, 21, 10, 0, 0),
                UserLogin = "jan",
                Action = "Login",
                Description = "Zalogowano."
            };

            await service.AddAsync(entry);

            var loaded = await service.LoadAsync();

            Assert.Single(loaded);
            Assert.Equal("jan", loaded[0].UserLogin);
            Assert.Equal("Login", loaded[0].Action);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LoadAsync_ReturnsSavedEntries()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();

        try
        {
            var service = new LocalAuditLogService(directory);

            await service.AddAsync(new AuditLogEntry { UserLogin = "a", Action = "A", Description = "1" });
            await service.AddAsync(new AuditLogEntry { UserLogin = "b", Action = "B", Description = "2" });

            var loaded = await service.LoadAsync();

            Assert.Equal(2, loaded.Count);
            Assert.Equal("a", loaded[0].UserLogin);
            Assert.Equal("b", loaded[1].UserLogin);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LoadForTicketAsync_FiltersByTicketId()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();

        try
        {
            var service = new LocalAuditLogService(directory);

            await service.AddAsync(new AuditLogEntry
            {
                UserLogin = "it",
                Action = "Update",
                TicketId = 10,
                Description = "Ticket 10"
            });
            await service.AddAsync(new AuditLogEntry
            {
                UserLogin = "it",
                Action = "Update",
                TicketId = 20,
                Description = "Ticket 20"
            });
            await service.AddAsync(new AuditLogEntry
            {
                UserLogin = "it",
                Action = "Login",
                Description = "Bez ticketu"
            });

            var forTicket10 = await service.LoadForTicketAsync(10);

            Assert.Single(forTicket10);
            Assert.Equal(10, forTicket10[0].TicketId);
            Assert.Equal("Ticket 10", forTicket10[0].Description);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();

        try
        {
            var service = new LocalAuditLogService(directory);

            await service.AddAsync(new AuditLogEntry { UserLogin = "x", Action = "Test", Description = "d" });
            await service.ClearAsync();

            var loaded = await service.LoadAsync();

            Assert.Empty(loaded);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LoadAsync_WithCorruptedFile_ReturnsEmptyList()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();

        try
        {
            var service = new LocalAuditLogService(directory);
            var filePath = Path.Combine(directory, "audit-log.json");
            await File.WriteAllTextAsync(filePath, "{ this is not valid audit json");

            var loaded = await service.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Empty(loaded);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }
}
