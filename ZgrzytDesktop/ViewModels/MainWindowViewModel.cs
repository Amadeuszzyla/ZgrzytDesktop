using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
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
        IUserAdminService userAdminService)
        : this(
            new MainWindowDependencies(
                authService,
                ticketService,
                settingsService,
                ticketCacheService,
                userCacheService,
                auditLogService,
                userAdminService),
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

        _currentViewModel = new LoginViewModel(_authService, _auditLogService, OnLoginSuccess);

        if (runStartup)
            _ = TryAutoLoginAsync();
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
            IUserAdminService userAdminService)
        {
            AuthService = authService;
            TicketService = ticketService;
            SettingsService = settingsService;
            TicketCacheService = ticketCacheService;
            UserCacheService = userCacheService;
            AuditLogService = auditLogService;
            UserAdminService = userAdminService;
        }

        public IAuthService AuthService { get; }

        public ITicketService TicketService { get; }

        public ISettingsService SettingsService { get; }

        public ILocalTicketCacheService TicketCacheService { get; }

        public ILocalUserCacheService UserCacheService { get; }

        public ILocalAuditLogService AuditLogService { get; }

        public IUserAdminService UserAdminService { get; }
    }

    private async Task TryAutoLoginAsync()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync();

            if (user is not null)
            {
                await _userCacheService.SaveUserAsync(user);
                await LogLoginAsync(user, "Automatyczne logowanie przy starcie aplikacji.");
                CurrentViewModel = CreateDashboard(user);
            }
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

        if (cachedUser is not null)
        {
            CurrentViewModel = CreateDashboard(cachedUser);
        }
    }

    private void OnLoginSuccess(User user)
    {
        _ = HandleLoginSuccessAsync(user);
    }

    private async Task HandleLoginSuccessAsync(User user)
    {
        await _userCacheService.SaveUserAsync(user);
        CurrentViewModel = CreateDashboard(user);
    }

    private DashboardViewModel CreateDashboard(User user)
    {
        return new DashboardViewModel(
            user,
            _authService,
            _ticketService,
            _settingsService,
            _ticketCacheService,
            _auditLogService,
            _userAdminService,
            LogoutAsync
        );
    }

    private async Task LogLoginAsync(User user, string description)
    {
        await _auditLogService.AddAsync(new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            UserLogin = user.Login,
            Action = "Login",
            Description = description
        });
    }

    private async Task LogoutAsync()
    {
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

        await _auditLogService.AddAsync(new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            UserLogin = userLogin,
            Action = "Logout",
            Description = "Wylogowano użytkownika z aplikacji desktopowej."
        });

        await _userCacheService.ClearAsync();
        await _ticketCacheService.ClearAsync();

        CurrentViewModel = new LoginViewModel(_authService, _auditLogService, OnLoginSuccess);
    }
}