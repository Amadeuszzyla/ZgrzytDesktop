using System.Threading.Tasks;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Storage;

namespace ZgrzytDesktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly TicketService _ticketService;
    private readonly ApiService _apiService;
    private readonly SettingsService _settingsService;
    private readonly LocalTicketCacheService _ticketCacheService;
    private readonly LocalUserCacheService _userCacheService;

    private ViewModelBase _currentViewModel;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public MainWindowViewModel()
    {
        var tokenStorage = new TokenStorage();

        _settingsService = new SettingsService();
        _apiService = new ApiService(tokenStorage, _settingsService);

        _authService = new AuthService(_apiService, tokenStorage);
        _ticketService = new TicketService(_apiService);
        _ticketCacheService = new LocalTicketCacheService();
        _userCacheService = new LocalUserCacheService();

        _currentViewModel = new LoginViewModel(_authService, OnLoginSuccess);

        _ = TryAutoLoginAsync();
    }

    private async Task TryAutoLoginAsync()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync();

            if (user is not null)
            {
                await _userCacheService.SaveUserAsync(user);
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
            _ticketService,
            _apiService,
            _settingsService,
            _ticketCacheService,
            LogoutAsync
        );
    }

    private async Task LogoutAsync()
    {
        try
        {
            await _authService.LogoutAsync();
        }
        catch
        {
            // Nawet jeśli backend nie odpowie przy wylogowaniu,
            // lokalnie czyścimy dane i wracamy do logowania.
        }

        await _userCacheService.ClearAsync();
        await _ticketCacheService.ClearAsync();

        CurrentViewModel = new LoginViewModel(_authService, OnLoginSuccess);
    }
}