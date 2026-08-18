using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public ProductService(IUnitOfWork unitOfWork, IInventoryService inventoryService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public async Task<(List<ProductListItemDto> Items, int TotalCount)> SearchAsync(string? searchText, int? categoryId, bool activeOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (products, total) = await _unitOfWork.Products.SearchAsync(searchText, categoryId, activeOnly, pageNumber, pageSize, cancellationToken);
        return (products.Select(ToListItem).ToList(), total);
    }

    public async Task<ProductDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        return product is null ? null : ToDetail(product);
    }

    public async Task<ProductDetailDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByBarcodeAsync(barcode, cancellationToken);
        return product is null ? null : ToDetail(product);
    }

    public async Task<List<ProductListItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default) =>
        (await _unitOfWork.Products.GetLowStockAsync(cancellationToken)).Select(ToListItem).ToList();

    public async Task<List<ProductListItemDto>> GetOutOfStockAsync(CancellationToken cancellationToken = default) =>
        (await _unitOfWork.Products.GetOutOfStockAsync(cancellationToken)).Select(ToListItem).ToList();

    public async Task<List<ProductPriceHistoryDto>> GetPriceHistoryAsync(int productId, CancellationToken cancellationToken = default)
    {
        var history = await _unitOfWork.Products.GetPriceHistoryAsync(productId, cancellationToken);

        return history.Select(h => new ProductPriceHistoryDto
        {
            ChangedDate = h.ChangedDate,
            ChangedByUserName = h.ChangedByUser?.FullName ?? string.Empty,
            OldPurchasePrice = h.OldPurchasePrice,
            NewPurchasePrice = h.NewPurchasePrice,
            OldSellingPrice = h.OldSellingPrice,
            NewSellingPrice = h.NewSellingPrice,
            OldWholesalePrice = h.OldWholesalePrice,
            NewWholesalePrice = h.NewWholesalePrice,
            Reason = h.Reason.ToString()
        }).ToList();
    }

    public async Task<int> CreateAsync(CreateProductDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        Validate(dto.Name, dto.Barcode, dto.SKU, dto.PurchasePrice, dto.SellingPrice, dto.WholesalePrice);

        if (await _unitOfWork.Products.BarcodeExistsAsync(dto.Barcode, null, cancellationToken))
        {
            throw new DuplicateBarcodeException(dto.Barcode);
        }
        if (await _unitOfWork.Products.SkuExistsAsync(dto.SKU, null, cancellationToken))
        {
            throw new DuplicateSkuException(dto.SKU);
        }
        if (dto.OpeningStock < 0)
        {
            throw new ValidationAppException("Opening stock cannot be negative.");
        }

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Barcode = dto.Barcode.Trim(),
            SKU = dto.SKU.Trim(),
            CategoryId = dto.CategoryId,
            PurchasePrice = dto.PurchasePrice,
            SellingPrice = dto.SellingPrice,
            WholesalePrice = dto.WholesalePrice,
            MinimumStockLevel = dto.MinimumStockLevel,
            ReorderLevel = dto.ReorderLevel,
            Description = dto.Description,
            HasExpiry = dto.HasExpiry,
            CurrentStock = 0,
            IsActive = true
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _unitOfWork.Products.Add(product);
            await _unitOfWork.SaveChangesAsync(ct); // assigns product.Id for the FK below

            if (dto.OpeningStock > 0)
            {
                await _inventoryService.PostTransactionAsync(
                    product, InventoryTransactionType.OpeningStock, dto.OpeningStock, dto.PurchasePrice,
                    ReferenceType.OpeningBalance, product.Id, currentUserId,
                    "Opening stock recorded at product creation.", cancellationToken: ct);
            }
        }, cancellationToken);

        await _auditService.LogAsync(currentUserId, "ProductCreated", "Product", product.Id, null, null, $"Created product '{product.Name}' (Barcode: {product.Barcode}).", cancellationToken);
        return product.Id;
    }

    public async Task UpdateAsync(UpdateProductDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        Validate(dto.Name, dto.Barcode, dto.SKU, dto.PurchasePrice, dto.SellingPrice, dto.WholesalePrice);

        var product = await _unitOfWork.Products.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new ValidationAppException("Product not found.");

        if (await _unitOfWork.Products.BarcodeExistsAsync(dto.Barcode, dto.Id, cancellationToken))
        {
            throw new DuplicateBarcodeException(dto.Barcode);
        }
        if (await _unitOfWork.Products.SkuExistsAsync(dto.SKU, dto.Id, cancellationToken))
        {
            throw new DuplicateSkuException(dto.SKU);
        }

        var pricesChanged = product.PurchasePrice != dto.PurchasePrice
                          || product.SellingPrice != dto.SellingPrice
                          || product.WholesalePrice != dto.WholesalePrice;

        if (pricesChanged)
        {
            product.PriceHistory.Add(new ProductPriceHistory
            {
                ProductId = product.Id,
                OldPurchasePrice = product.PurchasePrice,
                NewPurchasePrice = dto.PurchasePrice,
                OldSellingPrice = product.SellingPrice,
                NewSellingPrice = dto.SellingPrice,
                OldWholesalePrice = product.WholesalePrice,
                NewWholesalePrice = dto.WholesalePrice,
                ChangedByUserId = currentUserId,
                ChangedDate = DateTime.UtcNow,
                Reason = dto.PriceChangeReason,
                Notes = dto.PriceChangeNotes
            });
        }

        product.Name = dto.Name.Trim();
        product.Barcode = dto.Barcode.Trim();
        product.SKU = dto.SKU.Trim();
        product.CategoryId = dto.CategoryId;
        product.PurchasePrice = dto.PurchasePrice;
        product.SellingPrice = dto.SellingPrice;
        product.WholesalePrice = dto.WholesalePrice;
        product.MinimumStockLevel = dto.MinimumStockLevel;
        product.ReorderLevel = dto.ReorderLevel;
        product.Description = dto.Description;
        product.HasExpiry = dto.HasExpiry;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, "ProductUpdated", "Product", product.Id, null, null,
            pricesChanged ? $"Updated product '{product.Name}' (price change recorded)." : $"Updated product '{product.Name}'.", cancellationToken);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new ValidationAppException("Product not found.");

        product.IsActive = false;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(_currentUser.UserId ?? 0, "ProductDeactivated", "Product", product.Id, null, null, $"Deactivated product '{product.Name}'.", cancellationToken);
    }

    public async Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new ValidationAppException("Product not found.");

        product.IsActive = true;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(_currentUser.UserId ?? 0, "ProductReactivated", "Product", product.Id, null, null, $"Reactivated product '{product.Name}'.", cancellationToken);
    }

    private static void Validate(string name, string barcode, string sku, decimal purchasePrice, decimal sellingPrice, decimal wholesalePrice)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationAppException("Product name is required.");
        if (string.IsNullOrWhiteSpace(barcode)) throw new ValidationAppException("Barcode is required.");
        if (string.IsNullOrWhiteSpace(sku)) throw new ValidationAppException("SKU is required.");
        if (purchasePrice < 0) throw new ValidationAppException("Purchase price cannot be negative.");
        if (sellingPrice < 0) throw new ValidationAppException("Selling price cannot be negative.");
        if (wholesalePrice < 0) throw new ValidationAppException("Wholesale price cannot be negative.");
    }

    private static ProductListItemDto ToListItem(Product p) => new()
    {
        Id = p.Id,
        Barcode = p.Barcode,
        SKU = p.SKU,
        Name = p.Name,
        CategoryName = p.Category?.Name ?? string.Empty,
        PurchasePrice = p.PurchasePrice,
        SellingPrice = p.SellingPrice,
        CurrentStock = p.CurrentStock,
        MinimumStockLevel = p.MinimumStockLevel,
        IsActive = p.IsActive
    };

    private static ProductDetailDto ToDetail(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Barcode = p.Barcode,
        SKU = p.SKU,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? string.Empty,
        PurchasePrice = p.PurchasePrice,
        SellingPrice = p.SellingPrice,
        WholesalePrice = p.WholesalePrice,
        CurrentStock = p.CurrentStock,
        MinimumStockLevel = p.MinimumStockLevel,
        ReorderLevel = p.ReorderLevel,
        Description = p.Description,
        HasExpiry = p.HasExpiry,
        IsActive = p.IsActive
    };
}
