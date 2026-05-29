using System.Threading.Tasks;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public RequestAccountPanelViewModel RequestAccountPanel { get; private set; } = null!;

    public string RequestName
    {
        get => RequestAccountPanel.Name;
        set => RequestAccountPanel.Name = value;
    }

    public string RequestLogin
    {
        get => RequestAccountPanel.Login;
        set => RequestAccountPanel.Login = value;
    }

    public string RequestEmail
    {
        get => RequestAccountPanel.Email;
        set => RequestAccountPanel.Email = value;
    }

    public string RequestPassword
    {
        get => RequestAccountPanel.Password;
        set => RequestAccountPanel.Password = value;
    }

    public string RequestPasswordConfirmation
    {
        get => RequestAccountPanel.PasswordConfirmation;
        set => RequestAccountPanel.PasswordConfirmation = value;
    }

    public string RequestAccountStatusMessage
    {
        get => RequestAccountPanel.StatusMessage;
        set => RequestAccountPanel.StatusMessage = value;
    }

    public bool IsRequestingAccount => RequestAccountPanel.IsSubmitting;

    public bool CanRequestAccount => RequestAccountPanel.CanSubmit;

    private void InitializeRequestAccountPanel()
    {
        RequestAccountPanel = new RequestAccountPanelViewModel(
            _authService,
            _dashboardContext,
            () => CanUseOnlineActions);

        RequestAccountPanel.PropertyChanged += (_, e) =>
        {
            var forwarded = e.PropertyName switch
            {
                nameof(RequestAccountPanelViewModel.Name) => nameof(RequestName),
                nameof(RequestAccountPanelViewModel.Login) => nameof(RequestLogin),
                nameof(RequestAccountPanelViewModel.Email) => nameof(RequestEmail),
                nameof(RequestAccountPanelViewModel.Password) => nameof(RequestPassword),
                nameof(RequestAccountPanelViewModel.PasswordConfirmation) => nameof(RequestPasswordConfirmation),
                nameof(RequestAccountPanelViewModel.StatusMessage) => nameof(RequestAccountStatusMessage),
                nameof(RequestAccountPanelViewModel.IsSubmitting) => nameof(IsRequestingAccount),
                nameof(RequestAccountPanelViewModel.CanSubmit) => nameof(CanRequestAccount),
                _ => null
            };

            if (forwarded is not null)
                OnPropertyChanged(forwarded);
        };
    }
}
