using System.Threading.Tasks;



namespace ZgrzytDesktop.ViewModels;



public partial class DashboardViewModel

{

    private async Task AutoRefreshTicketsAsync() =>

        await TicketsPanel.AutoRefreshAsync(

            () => CurrentSection,

            message => PollingStatusMessage = message);

}

