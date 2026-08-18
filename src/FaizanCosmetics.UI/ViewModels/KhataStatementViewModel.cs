using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class KhataStatementViewModel : ViewModelBase
{
    private readonly IClientLedgerService _clientLedgerService;
    private int _clientId;

    public KhataStatementViewModel(IClientLedgerService clientLedgerService)
    {
        _clientLedgerService = clientLedgerService;
    }

    public ObservableCollection<ClientLedgerEntryDto> Entries { get; } = new();

    [ObservableProperty] private string clientName = string.Empty;
    [ObservableProperty] private decimal openingBalance;
    [ObservableProperty] private decimal closingBalance;
    [ObservableProperty] private DateTime? fromDate;
    [ObservableProperty] private DateTime? toDate;

    public async Task InitializeAsync(int clientId)
    {
        _clientId = clientId;
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
            // ToDate is inclusive of the whole day the user picked.
            var toDateInclusive = ToDate?.Date.AddDays(1).AddTicks(-1);
            var statement = await _clientLedgerService.GetStatementAsync(_clientId, FromDate, toDateInclusive);

            ClientName = statement.ClientName;
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
