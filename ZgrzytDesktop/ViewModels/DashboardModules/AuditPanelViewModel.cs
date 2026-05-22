using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed class AuditPanelViewModel : ViewModelBase
{
    private readonly ILocalAuditLogService _auditLogService;
    private readonly Action<string, string> _showToast;

    public AuditPanelViewModel(ILocalAuditLogService auditLogService, Action<string, string> showToast)
    {
        _auditLogService = auditLogService;
        _showToast = showToast;

        LoadAuditLogsCommand = new AsyncRelayCommand(RefreshAsync);
        ClearAuditLogsCommand = new AsyncRelayCommand(ClearAsync);
    }

    public ObservableCollection<AuditLogEntry> AuditLogEntries { get; } = new();

    public bool HasNoAuditLogEntries => AuditLogEntries.Count == 0;

    public IAsyncRelayCommand LoadAuditLogsCommand { get; }

    public IAsyncRelayCommand ClearAuditLogsCommand { get; }

    public async Task RefreshAsync()
    {
        var entries = await _auditLogService.LoadAsync();

        AuditLogEntries.Clear();

        foreach (var entry in entries.OrderByDescending(e => e.Timestamp))
            AuditLogEntries.Add(entry);

        OnPropertyChanged(nameof(HasNoAuditLogEntries));
    }

    private async Task ClearAsync()
    {
        await _auditLogService.ClearAsync();
        await RefreshAsync();
        _showToast("Lokalny audyt został wyczyszczony.", ToastTypes.Info);
    }
}
