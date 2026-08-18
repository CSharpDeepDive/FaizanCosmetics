using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.UI.ViewModels;

public partial class ProductEditViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ICurrentUserService _currentUser;

    public ProductEditViewModel(IProductService productService, ICategoryService categoryService, ICurrentUserService currentUser)
    {
        _productService = productService;
        _categoryService = categoryService;
        _currentUser = currentUser;
    }

    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public IReadOnlyList<PriceChangeReason> PriceChangeReasons { get; } = Enum.GetValues<PriceChangeReason>();

    [ObservableProperty] private int? productId;
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private string windowTitle = "Add Product";

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string barcode = string.Empty;
    [ObservableProperty] private string sku = string.Empty;
    [ObservableProperty] private CategoryDto? selectedCategory;
    [ObservableProperty] private decimal purchasePrice;
    [ObservableProperty] private decimal sellingPrice;
    [ObservableProperty] private decimal wholesalePrice;
    [ObservableProperty] private decimal minimumStockLevel;
    [ObservableProperty] private decimal reorderLevel;
    [ObservableProperty] private decimal openingStock;
    [ObservableProperty] private decimal currentStock;
    [ObservableProperty] private string? description;
    [ObservableProperty] private bool hasExpiry;
    [ObservableProperty] private PriceChangeReason selectedPriceChangeReason = PriceChangeReason.Correction;
    [ObservableProperty] private string? priceChangeNotes;

    /// <summary>True once the dialog closed after a successful save — the code-behind checks this to decide DialogResult.</summary>
    public bool SavedSuccessfully { get; private set; }

    public async Task InitializeAsync(int? existingProductId)
    {
        ProductId = existingProductId;
        IsEditMode = existingProductId.HasValue;
        WindowTitle = IsEditMode ? "Edit Product" : "Add Product";

        var categories = await _categoryService.GetAllAsync(activeOnly: true);
        Categories.Clear();
        foreach (var category in categories) Categories.Add(category);

        if (IsEditMode)
        {
            var product = await _productService.GetByIdAsync(existingProductId!.Value);
            if (product is null)
            {
                ErrorMessage = "Product not found.";
                return;
            }

            Name = product.Name;
            Barcode = product.Barcode;
            Sku = product.SKU;
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
            PurchasePrice = product.PurchasePrice;
            SellingPrice = product.SellingPrice;
            WholesalePrice = product.WholesalePrice;
            MinimumStockLevel = product.MinimumStockLevel;
            ReorderLevel = product.ReorderLevel;
            CurrentStock = product.CurrentStock;
            Description = product.Description;
            HasExpiry = product.HasExpiry;
        }
        else
        {
            SelectedCategory = Categories.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (SelectedCategory is null)
        {
            ErrorMessage = "Please select a category.";
            return;
        }

        IsBusy = true;
        try
        {
            var currentUserId = _currentUser.UserId ?? throw new ValidationAppException("No user is signed in.");

            if (IsEditMode)
            {
                await _productService.UpdateAsync(new UpdateProductDto
                {
                    Id = ProductId!.Value,
                    Name = Name,
                    Barcode = Barcode,
                    SKU = Sku,
                    CategoryId = SelectedCategory.Id,
                    PurchasePrice = PurchasePrice,
                    SellingPrice = SellingPrice,
                    WholesalePrice = WholesalePrice,
                    MinimumStockLevel = MinimumStockLevel,
                    ReorderLevel = ReorderLevel,
                    Description = Description,
                    HasExpiry = HasExpiry,
                    PriceChangeReason = SelectedPriceChangeReason,
                    PriceChangeNotes = PriceChangeNotes
                }, currentUserId);
            }
            else
            {
                await _productService.CreateAsync(new CreateProductDto
                {
                    Name = Name,
                    Barcode = Barcode,
                    SKU = Sku,
                    CategoryId = SelectedCategory.Id,
                    PurchasePrice = PurchasePrice,
                    SellingPrice = SellingPrice,
                    WholesalePrice = WholesalePrice,
                    MinimumStockLevel = MinimumStockLevel,
                    ReorderLevel = ReorderLevel,
                    Description = Description,
                    HasExpiry = HasExpiry,
                    OpeningStock = OpeningStock
                }, currentUserId);
            }

            SavedSuccessfully = true;
            RequestClose?.Invoke();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to save the product right now. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    /// <summary>Lets the code-behind close the window without the ViewModel taking a Window dependency.</summary>
    public event Action? RequestClose;
}
