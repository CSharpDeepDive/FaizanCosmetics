using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace FaizanCosmetics.UI.ViewModels;

public partial class SalesHistoryViewModel : ViewModelBase
{
    private const int PageSize = 50;

    private readonly ISalesInvoiceService _salesInvoiceService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    public SalesHistoryViewModel(ISalesInvoiceService salesInvoiceService, ICurrentUserService currentUser, IServiceProvider serviceProvider)
    {
        _salesInvoiceService = salesInvoiceService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;
        CanCancelInvoices = currentUser.Role is UserRole.Admin or UserRole.Manager;
        _ = LoadAsync();
    }

    public bool CanCancelInvoices { get; }

    public ObservableCollection<SalesInvoiceListItemDto> Invoices { get; } = new();
    public ObservableCollection<SalesInvoiceItemDetailDto> SelectedInvoiceItems { get; } = new();

    [ObservableProperty] private string? searchInvoiceNumber;
    [ObservableProperty] private DateTime? fromDate;
    [ObservableProperty] private DateTime? toDate;
    [ObservableProperty] private SalesInvoiceListItemDto? selectedInvoice;
    [ObservableProperty] private SalesInvoiceDetailDto? selectedInvoiceDetail;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private int totalCount;

    partial void OnSelectedInvoiceChanged(SalesInvoiceListItemDto? value) => _ = LoadDetailAsync();

    private async Task LoadDetailAsync()
    {
        SelectedInvoiceItems.Clear();
        SelectedInvoiceDetail = null;
        if (SelectedInvoice is null) return;

        var detail = await _salesInvoiceService.GetByIdAsync(SelectedInvoice.Id);
        if (detail is null) return;

        SelectedInvoiceDetail = detail;
        foreach (var item in detail.Items) SelectedInvoiceItems.Add(item);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages) { CurrentPage++; await LoadAsync(); }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1) { CurrentPage--; await LoadAsync(); }
    }

    [RelayCommand]
    private void CancelInvoice()
    {
        if (SelectedInvoice is null || SelectedInvoice.Status != InvoiceStatus.Posted) return;

        var dialog = _serviceProvider.GetRequiredService<Views.ReasonPromptWindow>();
        dialog.Initialize("Cancel Invoice", $"Cancelling invoice {SelectedInvoice.InvoiceNumber} will restore stock and reverse any Khata charge. This cannot be undone.");
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.EnteredReason))
        {
            _ = CancelInternalAsync(dialog.EnteredReason);
        }
    }

    private async Task CancelInternalAsync(string reason)
    {
        ErrorMessage = null;
        try
        {
            await _salesInvoiceService.CancelAsync(SelectedInvoice!.Id, reason, _currentUser.UserId ?? 0);
            await LoadAsync();
            await LoadDetailAsync();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var toDateInclusive = ToDate?.Date.AddDays(1).AddTicks(-1);
            var (items, total) = await _salesInvoiceService.SearchAsync(SearchInvoiceNumber, FromDate, toDateInclusive, CurrentPage, PageSize);

            Invoices.Clear();
            foreach (var item in items) Invoices.Add(item);

            TotalCount = total;
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to load sales history. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
