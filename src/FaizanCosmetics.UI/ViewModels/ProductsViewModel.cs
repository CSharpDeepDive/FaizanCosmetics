using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FaizanCosmetics.UI.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    private const int PageSize = 50;

    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;

    public ProductsViewModel(IProductService productService, ICategoryService categoryService, IServiceProvider serviceProvider, INavigationService navigationService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;

        _ = InitializeAsync();
    }

    public ObservableCollection<ProductListItemDto> Products { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private CategoryDto? selectedCategory;
    [ObservableProperty] private ProductListItemDto? selectedProduct;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private int totalCount;

    private bool _suppressCategoryReload;

    private async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.GetAllAsync(activeOnly: true);
        Categories.Clear();
        Categories.Add(new CategoryDto { Id = 0, Name = "All Categories" });
        foreach (var category in categories) Categories.Add(category);

        _suppressCategoryReload = true;
        SelectedCategory = Categories.First();
        _suppressCategoryReload = false;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private void AddProduct()
    {
        var dialog = _serviceProvider.GetRequiredService<Views.ProductEditWindow>();
        dialog.Initialize(productId: null);
        if (dialog.ShowDialog() == true)
        {
            _ = LoadProductsAsync();
        }
    }

    [RelayCommand]
    private void EditProduct()
    {
        if (SelectedProduct is null) return;

        var dialog = _serviceProvider.GetRequiredService<Views.ProductEditWindow>();
        dialog.Initialize(productId: SelectedProduct.Id);
        if (dialog.ShowDialog() == true)
        {
            _ = LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedProduct is null) return;

        try
        {
            if (SelectedProduct.IsActive)
            {
                await _productService.DeactivateAsync(SelectedProduct.Id);
            }
            else
            {
                await _productService.ReactivateAsync(SelectedProduct.Id);
            }
            await LoadProductsAsync();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void ViewPriceHistory()
    {
        if (SelectedProduct is null) return;

        var dialog = _serviceProvider.GetRequiredService<Views.PriceHistoryWindow>();
        dialog.Initialize(SelectedProduct.Id, SelectedProduct.Name);
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void OpenCategories()
    {
        var dialog = _serviceProvider.GetRequiredService<Views.CategoriesWindow>();
        dialog.ShowDialog();
        _ = LoadCategoriesAsync();
        _ = LoadProductsAsync();
    }

    [RelayCommand]
    private void ViewLowStock() => _navigationService.NavigateTo<LowStockViewModel>();

    private async Task LoadProductsAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var categoryFilter = SelectedCategory is { Id: > 0 } ? SelectedCategory.Id : (int?)null;
            var (items, total) = await _productService.SearchAsync(SearchText, categoryFilter, activeOnly: false, CurrentPage, PageSize);

            Products.Clear();
            foreach (var item in items) Products.Add(item);

            TotalCount = total;
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to load products. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedCategoryChanged(CategoryDto? value)
    {
        if (!_suppressCategoryReload) _ = SearchAsync();
    }
}
