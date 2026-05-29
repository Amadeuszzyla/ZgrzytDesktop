using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ITicketService _ticketService;
    private readonly ISettingsService _settingsService;
    private readonly ILocalTicketCacheService _ticketCacheService;
    private readonly ILocalUserCacheService _userCacheService;
    private readonly ILocalAuditLogService _auditLogService;
    private readonly IUserAdminService _userAdminService;
    private readonly IApiService _apiService;

    private ViewModelBase _currentViewModel;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public MainWindowViewModel(
        IAuthService authService,
        ITicketService ticketService,
        ISettingsService settingsService,
        ILocalTicketCacheService ticketCacheService,
        ILocalUserCacheService userCacheService,
        ILocalAuditLogService auditLogService,
        IUserAdminService userAdminService,
        IApiService apiService)
        : this(
            new MainWindowDependencies(
                authService,
                ticketService,
                settingsService,
                ticketCacheService,
                userCacheService,
                auditLogService,
                userAdminService,
                apiService),
            runStartup: true)
    {
    }

    internal MainWindowViewModel(MainWindowDependencies dependencies, bool runStartup = false)
    {
        _authService = dependencies.AuthService;
        _ticketService = dependencies.TicketService;
        _settingsService = dependencies.SettingsService;
        _ticketCacheService = dependencies.TicketCacheService;
        _userCacheService = dependencies.UserCacheService;
        _auditLogService = dependencies.AuditLogService;
        _userAdminService = dependencies.UserAdminService;
        _apiService = dependencies.ApiService;

        _currentViewModel = CreateLoginViewModel();

        if (runStartup)
            SafeFireAndForget.Run(TryAutoLoginAsync());
    }

    internal Task RunStartupAsync() => TryAutoLoginAsync();

    internal Task LogoutForTestsAsync() => LogoutAsync();

    internal sealed class MainWindowDependencies
    {
        public MainWindowDependencies(
            IAuthService authService,
            ITicketService ticketService,
            ISettingsService settingsService,
            ILocalTicketCacheService ticketCacheService,
            ILocalUserCacheService userCacheService,
            ILocalAuditLogService auditLogService,
            IUserAdminService userAdminService,
            IApiService apiService)
        {
            AuthService = authService;
            TicketService = ticketService;
            SettingsService = settingsService;
            TicketCacheService = ticketCacheService;
            UserCacheService = userCacheService;
            AuditLogService = auditLogService;
            UserAdminService = userAdminService;
            ApiService = apiService;
        }

        public IAuthService AuthService { get; }

        public ITicketService TicketService { get; }

        public ISettingsService SettingsService { get; }

        public ILocalTicketCacheService TicketCacheService { get; }

        public ILocalUserCacheService UserCacheService { get; }

        public ILocalAuditLogService AuditLogService { get; }

        public IUserAdminService UserAdminService { get; }
        public IApiService ApiService { get; }
    }

    private async Task TryAutoLoginAsync()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync();

            if (user is not null)
                await TryEnterApplicationAsync(user, "Audit_Desc_LoginAuto");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            await TryOpenOfflineDashboardAsync();
        }
        catch
        {
            // Brak tokenu, token wygasł albo użytkownik nie jest zalogowany.
            // Zostajemy na ekranie logowania.
        }

    }

    private async Task TryOpenOfflineDashboardAsync()
    {
        var cachedUser = await _userCacheService.LoadUserAsync();

        if (cachedUser is null)
            return;

        if (!DesktopAccessHelper.IsDesktopAccessAllowed(cachedUser.Role))
        {
            await DenyDesktopAccessAsync();
            return;
        }

        CurrentViewModel = CreateDashboard(cachedUser);
        RestartInactivityMonitor();
    }

    private void OnLoginSuccess(User user)
    {
        SafeFireAndForget.Run(HandleLoginSuccessAsync(user));
    }

    private async Task HandleLoginSuccessAsync(User user)
    {
        await TryEnterApplicationAsync(user, "Audit_Desc_LoginForm");
    }

    private async Task TryEnterApplicationAsync(User user, string? loginAuditDescription)
    {
        if (!DesktopAccessHelper.IsDesktopAccessAllowed(user.Role))
        {
            await DenyDesktopAccessAsync();
            return;
        }

        await _userCacheService.SaveUserAsync(user);

        if (!string.IsNullOrWhiteSpace(loginAuditDescription))
            await LogLoginAsync(user, loginAuditDescription);

        CurrentViewModel = CreateDashboard(user);
        RestartInactivityMonitor();
    }

    private async Task DenyDesktopAccessAsync()
    {
        try
        {
            await _authService.LogoutAsync();
        }
        catch
        {
            // Lokalnie i tak czyścimy sesję.
        }

        await _userCacheService.ClearAsync();
        await _ticketCacheService.ClearAsync();

        _sessionInactivityMonitor.Stop();
        CurrentViewModel = CreateLoginViewModel(AppStrings.Get("Login_DesktopAccessDenied"));
    }

    private LoginViewModel CreateLoginViewModel(string? initialErrorMessage = null) =>
        new(_authService, _auditLogService, OnLoginSuccess, initialErrorMessage);

    private DashboardViewModel CreateDashboard(User user)
    {
        var dashboard = new DashboardViewModel(
            user,
            _authService,
            _ticketService,
            _settingsService,
            _ticketCacheService,
            _auditLogService,
            _userAdminService,
            () => LogoutAsync(),
            ApplyAutoLogoutSettings
        );
        if (_apiService is ApiService apiService)
            apiService.OnSessionExpiredAsync = dashboard.HandleSessionExpiredFromApiAsync;
        return dashboard;
    }

    private async Task LogLoginAsync(User user, string detailsKey)
    {
        await _auditLogService.AddAsync(
            AuditLogEntryFactory.Create("Login", user.Login, ticketId: null, detailsKey));
    }

    private async Task LogoutAsync(string? loginErrorMessage = null)
    {
        if (CurrentViewModel is DashboardViewModel dashboard)
            dashboard.ClearSessionData();
        _sessionInactivityMonitor.Stop();
        var cachedUser = await _userCacheService.LoadUserAsync();
        var userLogin = cachedUser?.Login ?? "unknown";

        try
        {
            await _authService.LogoutAsync();
        }
        catch
        {
            // Nawet jeśli backend nie odpowie przy wylogowaniu,
            // lokalnie czyścimy dane i wracamy do logowania.
        }

        await _auditLogService.AddAsync(
            AuditLogEntryFactory.Create("Logout", userLogin, ticketId: null, "Audit_Desc_LogoutDesktop"));

        await _userCacheService.ClearAsync();
        await _ticketCacheService.ClearAsync();

        CurrentViewModel = CreateLoginViewModel(loginErrorMessage);
    }

}
