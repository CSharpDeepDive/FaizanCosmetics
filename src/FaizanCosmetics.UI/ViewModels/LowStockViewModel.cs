using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class LowStockViewModel : ViewModelBase
{
    private readonly IProductService _productService;

    public LowStockViewModel(IProductService productService)
    {
        _productService = productService;
        _ = LoadAsync();
    }

    public ObservableCollection<ProductListItemDto> LowStockProducts { get; } = new();
    public ObservableCollection<ProductListItemDto> OutOfStockProducts { get; } = new();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var lowStock = await _productService.GetLowStockAsync();
            var outOfStock = await _productService.GetOutOfStockAsync();

            LowStockProducts.Clear();
            foreach (var product in lowStock) LowStockProducts.Add(product);

            OutOfStockProducts.Clear();
            foreach (var product in outOfStock) OutOfStockProducts.Add(product);
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to load stock data. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
