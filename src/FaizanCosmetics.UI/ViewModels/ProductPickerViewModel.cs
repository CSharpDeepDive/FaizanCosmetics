using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class ProductPickerViewModel : ViewModelBase
{
    private readonly IProductService _productService;

    public ProductPickerViewModel(IProductService productService)
    {
        _productService = productService;
        _ = SearchAsync();
    }

    public ObservableCollection<ProductListItemDto> Products { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private ProductListItemDto? selectedProduct;

    public bool Confirmed { get; private set; }
    public event Action? RequestClose;

    [RelayCommand]
    private async Task SearchAsync()
    {
        var (items, _) = await _productService.SearchAsync(SearchText, null, activeOnly: true, 1, 50);
        Products.Clear();
        foreach (var item in items.Where(p => p.CurrentStock > 0)) Products.Add(item);
    }

    [RelayCommand]
    private void Select()
    {
        if (SelectedProduct is null) return;
        Confirmed = true;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
