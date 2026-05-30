using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Diagnostics;
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
    private readonly ILocalDiagnosticLogService _diagnosticLogService;
    private readonly IUserAdminService _userAdminService;
    private readonly IApiService _apiService;

    private ViewModelBase _currentViewModel;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetCurrentViewModel(value);
    }

    public MainWindowViewModel(
        IAuthService authService,
        ITicketService ticketService,
        ISettingsService settingsService,
        ILocalTicketCacheService ticketCacheService,
        ILocalUserCacheService userCacheService,
        ILocalAuditLogService auditLogService,
        IUserAdminService userAdminService,
        IApiService apiService,
        ILocalDiagnosticLogService diagnosticLogService)
        : this(
            new MainWindowDependencies(
                authService,
                ticketService,
                settingsService,
                ticketCacheService,
                userCacheService,
                auditLogService,
                userAdminService,
                apiService,
                diagnosticLogService),
            runStartup: true)
    {
    }

    internal MainWindowViewModel(MainWindowDependencies dependencies, bool runStartup = false)
    {
        using (StartupPerf.Measure("MainWindowViewModel ctor"))
        {
            _authService = dependencies.AuthService;
            _ticketService = dependencies.TicketService;
            _settingsService = dependencies.SettingsService;
            _ticketCacheService = dependencies.TicketCacheService;
            _userCacheService = dependencies.UserCacheService;
            _auditLogService = dependencies.AuditLogService;
            _userAdminService = dependencies.UserAdminService;
            _apiService = dependencies.ApiService;
            _diagnosticLogService = dependencies.DiagnosticLogService;
            _autoLoginColdStartHintDelay = dependencies.AutoLoginColdStartHintDelay;
            _autoLoginTimeout = dependencies.AutoLoginTimeout;

            CancelAutoLoginCommand = new RelayCommand(CancelAutoLogin);

            using (StartupPerf.Measure("Create LoginViewModel"))
                _currentViewModel = CreateLoginViewModel();

            AttachLoginViewModelHandlers(_currentViewModel);
        }

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
            IApiService apiService,
            ILocalDiagnosticLogService diagnosticLogService,
            TimeSpan? autoLoginColdStartHintDelay = null,
            TimeSpan? autoLoginTimeout = null)
        {
            AuthService = authService;
            TicketService = ticketService;
            SettingsService = settingsService;
            TicketCacheService = ticketCacheService;
            UserCacheService = userCacheService;
            AuditLogService = auditLogService;
            UserAdminService = userAdminService;
            ApiService = apiService;
            DiagnosticLogService = diagnosticLogService;
            AutoLoginColdStartHintDelay = autoLoginColdStartHintDelay ?? TimeSpan.FromSeconds(3);
            AutoLoginTimeout = autoLoginTimeout ?? TimeSpan.FromSeconds(20);
        }

        public IAuthService AuthService { get; }

        public ITicketService TicketService { get; }

        public ISettingsService SettingsService { get; }

        public ILocalTicketCacheService TicketCacheService { get; }

        public ILocalUserCacheService UserCacheService { get; }

        public ILocalAuditLogService AuditLogService { get; }

        public IUserAdminService UserAdminService { get; }
        public IApiService ApiService { get; }

        public ILocalDiagnosticLogService DiagnosticLogService { get; }

        public TimeSpan AutoLoginColdStartHintDelay { get; }

        public TimeSpan AutoLoginTimeout { get; }
    }

    private async Task TryAutoLoginAsync()
    {
        BeginAutoLogin();
        var sessionId = _autoLoginSessionId;
        var cancellationToken = _autoLoginCts?.Token ?? CancellationToken.None;

        try
        {
            using (StartupPerf.Measure("Auto-login flow"))
            {
                User? user;
                var timedOut = false;
                using (StartupPerf.Measure("Auto-login — GetCurrentUserAsync (API)"))
                {
                    (user, timedOut) = await WaitForAutoLoginUserAsync(
                        _authService.GetCurrentUserAsync(),
                        cancellationToken,
                        _autoLoginTimeout);
                }

                if (timedOut)
                {
                    AbandonAutoLoginSession();
                    _diagnosticLogService.LogWarning(
                        $"Auto-login timed out after {_autoLoginTimeout.TotalSeconds:0}s waiting for GET user.");
                    ApplyAutoLoginTimeoutMessage();
                    return;
                }

                if (user is null ||
                    cancellationToken.IsCancellationRequested ||
                    sessionId != _autoLoginSessionId)
                    return;

                await TryEnterApplicationAsync(user, "Audit_Desc_LoginAuto");
            }
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            if (!cancellationToken.IsCancellationRequested)
                await TryOpenOfflineDashboardAsync();
        }
        catch (OperationCanceledException)
        {
            // Auto-login cancelled by user.
        }
        catch
        {
            // Brak tokenu, token wygasł albo użytkownik nie jest zalogowany.
            // Zostajemy na ekranie logowania.
        }
        finally
        {
            EndAutoLogin();
            StartupPerf.NotifyAutoLoginFinished();
        }
    }

    private async Task TryOpenOfflineDashboardAsync()
    {
        User? cachedUser;
        using (StartupPerf.Measure("Offline fallback — load user cache"))
            cachedUser = await _userCacheService.LoadUserAsync();

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
        using (StartupPerf.Measure("Enter application flow"))
        {
            if (!DesktopAccessHelper.IsDesktopAccessAllowed(user.Role))
            {
                await DenyDesktopAccessAsync();
                return;
            }

            using (StartupPerf.Measure("Save user cache"))
                await _userCacheService.SaveUserAsync(user);

            if (!string.IsNullOrWhiteSpace(loginAuditDescription))
            {
                using (StartupPerf.Measure("Audit log — login entry"))
                    await LogLoginAsync(user, loginAuditDescription);
            }

            CurrentViewModel = CreateDashboard(user);
            RestartInactivityMonitor();
        }
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
        DashboardViewModel dashboard;
        using (StartupPerf.Measure("Create DashboardViewModel"))
        {
            dashboard = new DashboardViewModel(
                user,
                _authService,
                _ticketService,
                _settingsService,
                _ticketCacheService,
                _auditLogService,
                _userAdminService,
                () => LogoutAsync(),
                ApplyAutoLogoutSettings,
                _diagnosticLogService);
        }

        StartupPerf.MarkDashboardCreated();

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
