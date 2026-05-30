using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Diagnostics;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketsPanelViewModel
{
    public async Task LoadTicketsAsync(bool silentRefresh = false)
    {
        try
        {
            if (!silentRefresh)
            {
                IsLoading = true;
                StatusMessage = AppStrings.Get("Tickets_Loading");
            }

            PaginatedResponse<Ticket>? response;
            using (StartupPerf.Measure("LoadTicketsAsync — GetTicketsAsync (API)"))
            {
                response = await _ticketService.GetTicketsAsync(
                    page: CurrentPage,
                    perPage: PageSize,
                    search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                    status: GetSelectedFilterValue(SelectedFilterStatus),
                    priority: GetSelectedFilterValue(SelectedFilterPriority),
                    sortBy: SelectedTicketSortField?.SortBy ?? TicketSortHelper.DefaultField.SortBy,
                    sortDirection: SelectedTicketSortDirection?.Direction ?? TicketSortHelper.DefaultDirection.Direction,
                    queueView: GetSelectedTicketQueueView(),
                    categoryFilter: SelectedCategoryFilterKey,
                    assignmentFilter: SelectedAssignmentFilterKey,
                    currentUserId: _callbacks.GetCurrentUserId());
            }

            _callbacks.SetIsOffline(false);

            _allTickets.Clear();

            if (response?.Data is not null)
            {
                _allTickets.AddRange(response.Data);

                TotalTickets = response.Total;
                LastPage = Math.Max(1, (int)Math.Ceiling((double)TotalTickets / PageSize));

                if (CurrentPage > LastPage)
                {
                    SetCurrentPageSilently(LastPage);
                    await LoadTicketsAsync(silentRefresh);
                    return;
                }

                await _ticketCacheService.SaveTicketsAsync(_allTickets);

                ApplyVisibleTickets();
                _callbacks.NotifyStatistics(_allTickets, TotalTickets);

                StatusMessage = response.IsQueueFetchTruncated
                    ? AppStrings.GetFormat(
                        "Tickets_QueueFetchTruncated",
                        Math.Min(response.QueueApiReportedTotal ?? TotalTickets, TicketQueueFetchPolicy.MaxItems),
                        response.QueuePagesFetched)
                    : AppStrings.GetFormat("Tickets_Loaded", Tickets.Count, TotalTickets);
            }
            else
            {
                Tickets.Clear();
                TotalTickets = 0;
                LastPage = 1;
                UpdatePageNumbers();
                _callbacks.NotifyStatistics(_allTickets, TotalTickets);
                StatusMessage = AppStrings.Get("Tickets_NoTickets");
            }
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _callbacks.SetIsOffline(true);

            if (!silentRefresh)
                _callbacks.ShowToastKey("Toast_TicketsOfflineCache", ToastTypes.Warning);

            await LoadTicketsFromCacheAsync();
        }
        catch (ApiException ex)
        {
            StatusMessage = _callbacks.GetApiErrorMessage(ex);

            if (!silentRefresh)
                _callbacks.ShowToastRaw(_callbacks.GetApiErrorMessage(ex), ToastTypes.Error);
        }
        catch
        {
            StatusMessage = AppStrings.Get("Api_UnexpectedError");

            if (!silentRefresh)
                _callbacks.ShowToastKey("Toast_TicketsFetchError", ToastTypes.Error);
        }
        finally
        {
            if (!silentRefresh)
                IsLoading = false;

            RefreshPaginationProperties();
        }
    }

    public async Task LoadTicketsFromCacheAsync()
    {
        List<Ticket> cachedTickets;
        using (StartupPerf.Measure("LoadTicketsFromCacheAsync — read ticket cache"))
            cachedTickets = await _ticketCacheService.LoadTicketsAsync();
        var filteredTickets = TicketQueueListProcessor.Filter(
            cachedTickets,
            GetSelectedFilterValue(SelectedFilterStatus),
            GetSelectedFilterValue(SelectedFilterPriority),
            search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
            SelectedCategoryFilterKey,
            SelectedAssignmentFilterKey,
            _callbacks.GetCurrentUserId());

        _allTickets.Clear();
        _allTickets.AddRange(filteredTickets);

        TotalTickets = filteredTickets.Count;
        LastPage = Math.Max(1, (int)Math.Ceiling((double)TotalTickets / PageSize));

        if (CurrentPage > LastPage)
            SetCurrentPageSilently(LastPage);

        var pageItems = _allTickets
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Tickets.Clear();

        foreach (var ticket in pageItems)
            Tickets.Add(ticket);

        UpdatePageNumbers();

        StatusMessage = cachedTickets.Count > 0
            ? AppStrings.GetFormat("Tickets_OfflineShowingCache", Tickets.Count)
            : AppStrings.Get("Tickets_OfflineNoCache");

        _callbacks.NotifyStatistics(_allTickets, TotalTickets);
    }

    public async Task RefreshTicketsNowAsync() => await LoadTicketsAsync();

    public async Task AutoRefreshAsync(Func<string> getCurrentSection, Action<string> setPollingStatusMessage)
    {
        if (getCurrentSection() != AppSections.Tickets || _callbacks.GetIsOffline() || IsLoading)
            return;

        try
        {
            await LoadTicketsAsync(silentRefresh: true);
            _autoRefreshErrorToastShown = false;
            setPollingStatusMessage(AutoRefreshStatusText);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _callbacks.SetIsOffline(true);
            setPollingStatusMessage(AppStrings.Get("Tickets_OfflineAutoRefreshPaused"));

            if (!_autoRefreshErrorToastShown)
            {
                _autoRefreshErrorToastShown = true;
                _callbacks.ShowToastKey("Toast_TicketsLostConnection", ToastTypes.Warning);
            }
        }
        catch
        {
            setPollingStatusMessage(AppStrings.Get("Tickets_AutoRefreshFailed"));

            if (!_autoRefreshErrorToastShown)
            {
                _autoRefreshErrorToastShown = true;
                _callbacks.ShowToastKey("Toast_TicketsRefreshFailed", ToastTypes.Error);
            }
        }
    }

    private void ApplyVisibleTickets()
    {
        var filtered = TicketQueueListProcessor.Filter(
            _allTickets,
            GetSelectedFilterValue(SelectedFilterStatus),
            GetSelectedFilterValue(SelectedFilterPriority),
            search: null,
            SelectedCategoryFilterKey,
            SelectedAssignmentFilterKey,
            _callbacks.GetCurrentUserId());

        Tickets.Clear();

        foreach (var ticket in filtered)
            Tickets.Add(ticket);

        UpdatePageNumbers();
        RefreshPaginationProperties();
    }
}
