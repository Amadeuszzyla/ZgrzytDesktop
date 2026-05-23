using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using TicketMessage = ZgrzytDesktop.Models.Message;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketStatisticsCalculatorTests
{
    public TicketStatisticsCalculatorTests() => ViewModelTestSetup.EnsureAppStrings();

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

    [Fact]
    public void ComputeResponseTime_WithoutApiFieldOrMessages_IsUnavailable()
    {
        var tickets = TicketTestDataBuilder.CreateMixedStatisticsSet();

        var responseTime = TicketStatisticsCalculator.ComputeResponseTime(tickets);

        Assert.False(responseTime.IsAvailable);
        Assert.Null(responseTime.Average);
        Assert.Equal(0, responseTime.SampleCount);
        Assert.Equal(ResponseTimeSampleSource.None, responseTime.Source);
    }

    [Fact]
    public void ComputeResponseTime_WithFirstResponseAt_UsesApiField()
    {
        var created = new DateTime(2026, 1, 1, 10, 0, 0);
        var firstResponse = created.AddHours(2);

        var tickets = new List<Ticket>
        {
            new()
            {
                Id = 1,
                CreatedAt = created,
                FirstResponseAt = firstResponse
            },
            new()
            {
                Id = 2,
                CreatedAt = created,
                FirstResponseAt = created.AddHours(4)
            }
        };

        var responseTime = TicketStatisticsCalculator.ComputeResponseTime(tickets);

        Assert.True(responseTime.IsAvailable);
        Assert.Equal(2, responseTime.SampleCount);
        Assert.Equal(ResponseTimeSampleSource.FirstResponseAtField, responseTime.Source);
        Assert.Equal(TimeSpan.FromHours(3), responseTime.Average);
    }

    [Fact]
    public void ComputeResponseTime_WithEmbeddedStaffMessages_UsesFirstStaffMessage()
    {
        var created = new DateTime(2026, 2, 1, 9, 0, 0);

        var tickets = new List<Ticket>
        {
            new()
            {
                Id = 1,
                CreatedAt = created,
                Messages =
                [
                    new TicketMessage
                    {
                        CreatedAt = created.AddMinutes(30),
                        Sender = new User { Role = "user" }
                    },
                    new TicketMessage
                    {
                        CreatedAt = created.AddHours(1),
                        Sender = new User { Role = "it" }
                    }
                ]
            }
        };

        var responseTime = TicketStatisticsCalculator.ComputeResponseTime(tickets);

        Assert.True(responseTime.IsAvailable);
        Assert.Equal(TimeSpan.FromHours(1), responseTime.Average);
        Assert.Equal(ResponseTimeSampleSource.EmbeddedStaffMessages, responseTime.Source);
    }

    [Fact]
    public void ComputeResponseTime_PrefersFirstResponseAtOverMessages()
    {
        var created = new DateTime(2026, 3, 1, 8, 0, 0);

        var tickets = new List<Ticket>
        {
            new()
            {
                Id = 1,
                CreatedAt = created,
                FirstResponseAt = created.AddMinutes(15),
                Messages =
                [
                    new TicketMessage
                    {
                        CreatedAt = created.AddHours(2),
                        Sender = new User { Role = "it" }
                    }
                ]
            }
        };

        var responseTime = TicketStatisticsCalculator.ComputeResponseTime(tickets);

        Assert.Equal(TimeSpan.FromMinutes(15), responseTime.Average);
        Assert.Equal(ResponseTimeSampleSource.FirstResponseAtField, responseTime.Source);
    }
}
