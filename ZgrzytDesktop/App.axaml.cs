using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.Storage;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.Views;

namespace ZgrzytDesktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnDesktopExit;

            try
            {
                _serviceProvider = BuildServiceProvider();
                var shellViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = shellViewModel
                };
            }
            catch (Exception)
            {
                desktop.MainWindow = CreateErrorWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            _serviceProvider?.Dispose();
        }
        catch
        {
            // Dispose nie może blokować zamknięcia aplikacji.
        }
        finally
        {
            _serviceProvider = null;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITokenStorage, TokenStorage>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ApiService>();
        services.AddSingleton<IApiService>(sp => sp.GetRequiredService<ApiService>());
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ITicketService, TicketService>();
        services.AddSingleton<IUserAdminService, UserAdminService>();
        services.AddSingleton<ILocalAuditLogService, LocalAuditLogService>();
        services.AddSingleton<ILocalTicketCacheService, LocalTicketCacheService>();
        services.AddSingleton<ILocalUserCacheService, LocalUserCacheService>();
        services.AddSingleton<MainWindowViewModel>();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var apiService = provider.GetRequiredService<ApiService>();
        var authService = provider.GetRequiredService<IAuthService>();
        apiService.TryRefreshSessionAsync = () => authService.RefreshTokenAsync();

        var settingsService = provider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSync();
        AppStrings.ApplyCulture(settings.UiCulture);
        SettingsService.ApplyThemeMode(settings.ThemeMode);

        return provider;
    }

    private static MainWindow CreateErrorWindow()
    {
        return new MainWindow
        {
            Content = new Border
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
            }
        };
    }
}
