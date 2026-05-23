using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.ViewModels;
using TicketMessage = ZgrzytDesktop.Models.Message;

namespace ZgrzytDesktop.Tests.Models;

public class DisplayTextModelTests
{
    public DisplayTextModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void Ticket_DisplayDescription_StripsHtml()
    {
        var ticket = new Ticket { Description = "<p>Printer jam</p>" };

        Assert.Equal("Printer jam", ticket.DisplayDescription);
    }

    [Fact]
    public void Message_DisplayBody_StripsHtml()
    {
        var message = new TicketMessage { Content = "<strong>Hello</strong> IT" };

        Assert.Equal("Hello IT", message.DisplayBody);
    }

    [Fact]
    public void Message_DisplayBody_WhenEmpty_ShowsLocalizedFallback()
    {
        AppStrings.ApplyCulture("pl");
        var message = new TicketMessage { Content = "   " };

        Assert.Equal("Brak treści wiadomości.", message.DisplayBody);

        AppStrings.ApplyCulture("en");
        message = new TicketMessage { Content = "<p></p>" };

        Assert.Equal("Message body is empty.", message.DisplayBody);
    }
}
