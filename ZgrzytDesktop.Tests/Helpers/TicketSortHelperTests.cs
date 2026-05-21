using Xunit;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketSortHelperTests
{
    [Fact]
    public void DefaultField_ShouldUseCreatedAt()
    {
        Assert.Equal("created_at", TicketSortHelper.DefaultField.SortBy);
    }

    [Fact]
    public void DefaultDirection_ShouldBeDescending()
    {
        Assert.Equal("desc", TicketSortHelper.DefaultDirection.Direction);
    }

    [Fact]
    public void Fields_ShouldContainRequiredSortColumns()
    {
        Assert.Contains(TicketSortHelper.Fields, f => f.SortBy == "created_at");
        Assert.Contains(TicketSortHelper.Fields, f => f.SortBy == "title");
        Assert.Contains(TicketSortHelper.Fields, f => f.SortBy == "status");
        Assert.Contains(TicketSortHelper.Fields, f => f.SortBy == "priority");
    }
}
