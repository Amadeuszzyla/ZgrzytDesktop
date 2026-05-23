using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed class AuditPanelViewModel : ViewModelBase
{
    private readonly ILocalAuditLogService _auditLogService;

    public AuditPanelViewModel(ILocalAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        LoadAuditLogsCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<AuditLogEntry> AuditLogEntries { get; } = new();

    public bool HasNoAuditLogEntries => AuditLogEntries.Count == 0;

    public IAsyncRelayCommand LoadAuditLogsCommand { get; }

    public string LblAuditTitle => AppStrings.Get("Audit_Title");

    public string LblAuditSubtitle => AppStrings.Get("Audit_Subtitle");

    public string LblAuditRefresh => AppStrings.Get("Audit_Refresh");

    public string LblAuditEmpty => AppStrings.Get("Audit_Empty");

    public string LblAuditColumnTimestamp => AppStrings.Get("Audit_ColumnTimestamp");

    public string LblAuditColumnUser => AppStrings.Get("Audit_ColumnUser");

    public string LblAuditColumnAction => AppStrings.Get("Audit_ColumnAction");

    public string LblAuditColumnTicketId => AppStrings.Get("Audit_ColumnTicketId");

    public string LblAuditColumnDescription => AppStrings.Get("Audit_ColumnDescription");

    public void NotifyLocalization()
    {
        OnPropertyChanged(nameof(LblAuditTitle));
        OnPropertyChanged(nameof(LblAuditSubtitle));
        OnPropertyChanged(nameof(LblAuditRefresh));
        OnPropertyChanged(nameof(LblAuditEmpty));
        OnPropertyChanged(nameof(LblAuditColumnTimestamp));
        OnPropertyChanged(nameof(LblAuditColumnUser));
        OnPropertyChanged(nameof(LblAuditColumnAction));
        OnPropertyChanged(nameof(LblAuditColumnTicketId));
        OnPropertyChanged(nameof(LblAuditColumnDescription));
        RefreshEntryDisplayBindings();
    }

    public async Task RefreshAsync()
    {
        var entries = await _auditLogService.LoadAsync();

        AuditLogEntries.Clear();

        foreach (var entry in entries.OrderByDescending(e => e.Timestamp))
            AuditLogEntries.Add(entry);

        OnPropertyChanged(nameof(HasNoAuditLogEntries));
    }

    private void RefreshEntryDisplayBindings()
    {
        if (AuditLogEntries.Count == 0)
            return;

        var snapshot = AuditLogEntries.ToList();
        AuditLogEntries.Clear();

        foreach (var entry in snapshot)
            AuditLogEntries.Add(entry);
    }
}
