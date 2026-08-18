using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class PriceHistoryViewModel : ViewModelBase
{
    private readonly IProductService _productService;

    public PriceHistoryViewModel(IProductService productService)
    {
        _productService = productService;
    }

    [ObservableProperty] private string productName = string.Empty;
    public ObservableCollection<ProductPriceHistoryDto> History { get; } = new();

    public async Task InitializeAsync(int productId, string productDisplayName)
    {
        ProductName = productDisplayName;
        IsBusy = true;
        try
        {
            var history = await _productService.GetPriceHistoryAsync(productId);
            History.Clear();
            foreach (var entry in history) History.Add(entry);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
