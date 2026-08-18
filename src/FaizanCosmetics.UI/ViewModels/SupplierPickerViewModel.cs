using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class SupplierPickerViewModel : ViewModelBase
{
    private readonly ISupplierService _supplierService;

    public SupplierPickerViewModel(ISupplierService supplierService)
    {
        _supplierService = supplierService;
        _ = SearchAsync();
    }

    public ObservableCollection<SupplierListItemDto> Suppliers { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private SupplierListItemDto? selectedSupplier;

    public bool Confirmed { get; private set; }
    public event Action? RequestClose;

    [RelayCommand]
    private async Task SearchAsync()
    {
        var (items, _) = await _supplierService.SearchAsync(SearchText, 1, 50);
        Suppliers.Clear();
        foreach (var item in items.Where(s => s.IsActive)) Suppliers.Add(item);
    }

    [RelayCommand]
    private void Select()
    {
        if (SelectedSupplier is null) return;
        Confirmed = true;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
