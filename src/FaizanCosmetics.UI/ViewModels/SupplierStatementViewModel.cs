using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class SupplierStatementViewModel : ViewModelBase
{
    private readonly ISupplierLedgerService _supplierLedgerService;
    private int _supplierId;

    public SupplierStatementViewModel(ISupplierLedgerService supplierLedgerService)
    {
        _supplierLedgerService = supplierLedgerService;
    }

    public ObservableCollection<SupplierLedgerEntryDto> Entries { get; } = new();

    [ObservableProperty] private string supplierName = string.Empty;
    [ObservableProperty] private decimal openingBalance;
    [ObservableProperty] private decimal closingBalance;
    [ObservableProperty] private DateTime? fromDate;
    [ObservableProperty] private DateTime? toDate;

    public async Task InitializeAsync(int supplierId)
    {
        _supplierId = supplierId;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ApplyFilterAsync() => await LoadAsync();

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        FromDate = null;
        ToDate = null;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var toDateInclusive = ToDate?.Date.AddDays(1).AddTicks(-1);
            var statement = await _supplierLedgerService.GetStatementAsync(_supplierId, FromDate, toDateInclusive);

            SupplierName = statement.SupplierName;
            OpeningBalance = statement.OpeningBalance;
            ClosingBalance = statement.ClosingBalance;

            Entries.Clear();
            foreach (var entry in statement.Entries) Entries.Add(entry);
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to load the statement. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
