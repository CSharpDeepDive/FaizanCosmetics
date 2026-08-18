using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class ClientPickerViewModel : ViewModelBase
{
    private readonly IClientService _clientService;

    public ClientPickerViewModel(IClientService clientService)
    {
        _clientService = clientService;
        _ = SearchAsync();
    }

    public ObservableCollection<ClientListItemDto> Clients { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private ClientListItemDto? selectedClient;

    public bool Confirmed { get; private set; }
    public event Action? RequestClose;

    [RelayCommand]
    private async Task SearchAsync()
    {
        var (items, _) = await _clientService.SearchAsync(SearchText, 1, 50);
        Clients.Clear();
        foreach (var item in items.Where(c => c.IsActive)) Clients.Add(item);
    }

    [RelayCommand]
    private void Select()
    {
        if (SelectedClient is null) return;
        Confirmed = true;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
