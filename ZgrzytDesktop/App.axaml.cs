using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.Storage;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.Views;

namespace ZgrzytDesktop;

public partial class App : Application
{
    private IAuthService? _authService;
    private ITicketService? _ticketService;
    private ApiService? _apiService;
    private ISettingsService? _settingsService;
    private LocalTicketCacheService? _ticketCacheService;
    private LocalUserCacheService? _userCacheService;
    private ILocalAuditLogService? _auditLogService;
    private IUserAdminService? _userAdminService;

    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = new MainWindow
            {
                Content = CreateStartupContent()
            };

            desktop.MainWindow = _mainWindow;

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    InitializeServices();
                    ApplyThemeFromSettings();

                    ShowLoginView();

                    await TryAutoLoginAsync();
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeServices()
    {
        var tokenStorage = new TokenStorage();

        _settingsService = new SettingsService();
        var settings = _settingsService.LoadSync();
        ZgrzytDesktop.Resources.AppStrings.ApplyCulture(settings.UiCulture);

        _apiService = new ApiService(tokenStorage, _settingsService);

        _authService = new AuthService(_apiService, tokenStorage);
        _apiService.TryRefreshSessionAsync = () => _authService.RefreshTokenAsync();
        _ticketService = new TicketService(_apiService);
        _ticketCacheService = new LocalTicketCacheService();
        _userCacheService = new LocalUserCacheService();
        _auditLogService = new LocalAuditLogService();
        _userAdminService = new UserAdminService(_apiService);
    }

    private async Task LogLoginAsync(User user, string description)
    {
        if (_auditLogService is null)
            return;

        await _auditLogService.AddAsync(new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            UserLogin = user.Login,
            Action = "Login",
            Description = description
        });
    }

    private void ApplyThemeFromSettings()
    {
        if (_settingsService is null)
            return;

        var settings = _settingsService.LoadSync();
        SettingsService.ApplyThemeMode(settings.ThemeMode);
    }

    private async Task TryAutoLoginAsync()
    {
        if (_authService is null || _userCacheService is null)
            return;

        try
        {
            var user = await _authService.GetCurrentUserAsync();

            if (user is not null)
            {
                await _userCacheService.SaveUserAsync(user);
                await LogLoginAsync(user, "Automatyczne logowanie przy starcie aplikacji.");
                ShowDashboardView(user);
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
        if (_userCacheService is null)
            return;

        var cachedUser = await _userCacheService.LoadUserAsync();

        if (cachedUser is not null)
        {
            ShowDashboardView(cachedUser);
        }
    }

    private void ShowLoginView()
    {
        if (_mainWindow is null)
            return;

        if (_authService is null)
            throw new InvalidOperationException("AuthService nie został zainicjalizowany.");

        var loginViewModel = new LoginViewModel(
            _authService,
            _auditLogService!,
            user => _ = HandleLoginSuccessAsync(user)
        );

        _mainWindow.Content = new LoginView
        {
            DataContext = loginViewModel
        };
    }

    private async Task HandleLoginSuccessAsync(User user)
    {
        if (_userCacheService is not null)
        {
            await _userCacheService.SaveUserAsync(user);
        }

        ShowDashboardView(user);
    }

    private void ShowDashboardView(User user)
    {
        if (_mainWindow is null)
            return;

        if (_ticketService is null)
            throw new InvalidOperationException("TicketService nie został zainicjalizowany.");

        if (_apiService is null)
            throw new InvalidOperationException("ApiService nie został zainicjalizowany.");

        if (_settingsService is null)
            throw new InvalidOperationException("SettingsService nie został zainicjalizowany.");

        if (_ticketCacheService is null)
            throw new InvalidOperationException("LocalTicketCacheService nie został zainicjalizowany.");

        if (_auditLogService is null)
            throw new InvalidOperationException("LocalAuditLogService nie został zainicjalizowany.");

        if (_userAdminService is null)
            throw new InvalidOperationException("UserAdminService nie został zainicjalizowany.");

        var dashboardViewModel = new DashboardViewModel(
            user,
            _authService!,
            _ticketService,
            _apiService,
            _settingsService,
            _ticketCacheService,
            _auditLogService,
            _userAdminService,
            LogoutAsync
        );

        _mainWindow.Content = new DashboardView
        {
            DataContext = dashboardViewModel
        };
    }

    private async Task LogoutAsync()
    {
        try
        {
            if (_authService is not null)
            {
                await _authService.LogoutAsync();
            }
        }
        catch
        {
            // Nawet jeśli backend nie odpowie, lokalnie wracamy do logowania.
        }

        if (_userCacheService is not null)
        {
            await _userCacheService.ClearAsync();
        }

        if (_ticketCacheService is not null)
        {
            await _ticketCacheService.ClearAsync();
        }

        ShowLoginView();
    }

    private static Control CreateStartupContent()
    {
        return new Border
        {
            Padding = new Thickness(30),
            Child = new TextBlock
            {
                Text = "Uruchamianie aplikacji ZGRZYT...",
                FontSize = 22,
                TextWrapping = TextWrapping.Wrap
            }
        };
    }

    private void ShowError(Exception ex)
    {
        if (_mainWindow is null)
            return;

        _mainWindow.Content = new Border
        {
            Padding = new Thickness(30),
            Child = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = "Nie udało się uruchomić aplikacji. Sprawdź połączenie z API i ustawienia, a następnie uruchom program ponownie.",
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
    }
}