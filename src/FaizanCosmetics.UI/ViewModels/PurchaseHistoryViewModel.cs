using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class PurchaseHistoryViewModel : ViewModelBase
{
    private const int PageSize = 50;

    private readonly IPurchaseInvoiceService _purchaseInvoiceService;

    public PurchaseHistoryViewModel(IPurchaseInvoiceService purchaseInvoiceService)
    {
        _purchaseInvoiceService = purchaseInvoiceService;
        _ = LoadAsync();
    }

    public ObservableCollection<PurchaseInvoiceListItemDto> Invoices { get; } = new();
    public ObservableCollection<PurchaseInvoiceItemDetailDto> SelectedInvoiceItems { get; } = new();

    [ObservableProperty] private string? searchInvoiceNumber;
    [ObservableProperty] private DateTime? fromDate;
    [ObservableProperty] private DateTime? toDate;
    [ObservableProperty] private PurchaseInvoiceListItemDto? selectedInvoice;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private int totalCount;

    partial void OnSelectedInvoiceChanged(PurchaseInvoiceListItemDto? value) => _ = LoadDetailAsync();

    private async Task LoadDetailAsync()
    {
        SelectedInvoiceItems.Clear();
        if (SelectedInvoice is null) return;

        var detail = await _purchaseInvoiceService.GetByIdAsync(SelectedInvoice.Id);
        if (detail is null) return;

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

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var toDateInclusive = ToDate?.Date.AddDays(1).AddTicks(-1);
            var (items, total) = await _purchaseInvoiceService.SearchAsync(SearchInvoiceNumber, FromDate, toDateInclusive, CurrentPage, PageSize);

            Invoices.Clear();
            foreach (var item in items) Invoices.Add(item);

            TotalCount = total;
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to load purchase history. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
