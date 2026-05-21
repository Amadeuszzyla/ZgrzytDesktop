using System.Text.Json;
using Xunit;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Tests.Models;

public class UnbanUserRequestTests
{
    [Fact]
    public void Serialize_ShouldMapPasswordProperty()
    {
        var json = JsonSerializer.Serialize(new UnbanUserRequest { Password = "secret" });

        Assert.Contains("\"password\":\"secret\"", json, StringComparison.Ordinal);
    }
}
