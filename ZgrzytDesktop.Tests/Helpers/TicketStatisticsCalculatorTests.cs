using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketStatisticsCalculatorTests
{
    [Fact]
    public void Compute_CountsStatusPriorityAndAssignment()
    {
        var snapshot = TicketStatisticsCalculator.Compute(TicketTestDataBuilder.CreateMixedStatisticsSet());

        Assert.Equal(5, snapshot.Total);
        Assert.Equal(2, snapshot.New);
        Assert.Equal(1, snapshot.InProgress);
        Assert.Equal(2, snapshot.Closed);
        Assert.Equal(2, snapshot.LowPriority);
        Assert.Equal(2, snapshot.MediumPriority);
        Assert.Equal(1, snapshot.HighPriority);
        Assert.Equal(3, snapshot.Assigned);
        Assert.Equal(2, snapshot.Unassigned);
    }
}
