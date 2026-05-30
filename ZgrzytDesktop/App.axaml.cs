using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Diagnostics;
using ZgrzytDesktop.Models;
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
    private bool _serviceProviderDisposed;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnDesktopExit;
            desktop.ShutdownRequested += OnDesktopShutdownRequested;

            try
            {
                using (StartupPerf.Measure("OnFrameworkInitializationCompleted"))
                {
                    ServiceProvider provider;
                    using (StartupPerf.Measure("BuildServiceProvider"))
                        provider = BuildServiceProvider();

                    _serviceProvider = provider;

                    MainWindowViewModel shellViewModel;
                    using (StartupPerf.Measure("Resolve MainWindowViewModel"))
                        shellViewModel = provider.GetRequiredService<MainWindowViewModel>();

                    using (StartupPerf.Measure("Create MainWindow"))
                    {
                        desktop.MainWindow = new MainWindow
                        {
                            DataContext = shellViewModel
                        };
                    }
                }
            }
            catch (Exception)
            {
                desktop.MainWindow = CreateErrorWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopShutdownRequested(object? sender, ShutdownRequestedEventArgs e) =>
        DisposeServiceProvider();

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e) =>
        DisposeServiceProvider();

    private void DisposeServiceProvider()
    {
        if (_serviceProviderDisposed)
            return;

        _serviceProviderDisposed = true;

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
        services.AddSingleton<ILocalDiagnosticLogService, LocalDiagnosticLogService>();
        services.AddSingleton<ILocalTicketCacheService, LocalTicketCacheService>();
        services.AddSingleton<ILocalUserCacheService, LocalUserCacheService>();
        services.AddSingleton<MainWindowViewModel>();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        using (StartupPerf.Measure("ConfigureServices"))
            ConfigureServices(services);

        ServiceProvider provider;
        using (StartupPerf.Measure("ServiceCollection.BuildServiceProvider"))
            provider = services.BuildServiceProvider();

        using (StartupPerf.Measure("Wire session refresh callback"))
        {
            var apiService = provider.GetRequiredService<ApiService>();
            var authService = provider.GetRequiredService<IAuthService>();
            apiService.TryRefreshSessionAsync = () => authService.RefreshTokenAsync();
        }

        AppSettings settings;
        using (StartupPerf.Measure("Load settings (App bootstrap)"))
        {
            var settingsService = provider.GetRequiredService<ISettingsService>();
            settings = settingsService.LoadSync();
        }

        using (StartupPerf.Measure("Apply culture and theme"))
        {
            AppStrings.ApplyCulture(settings.UiCulture);
            SettingsService.ApplyThemeMode(settings.ThemeMode);
        }

        var diagnosticLog = provider.GetRequiredService<ILocalDiagnosticLogService>();
        DiagnosticLogBridge.Service = diagnosticLog;
        StartupPerf.AttachLogger(diagnosticLog);

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
