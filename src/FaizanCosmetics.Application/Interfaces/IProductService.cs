using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

public interface IProductService
{
    Task<(List<ProductListItemDto> Items, int TotalCount)> SearchAsync(string? searchText, int? categoryId, bool activeOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<List<ProductListItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<List<ProductListItemDto>> GetOutOfStockAsync(CancellationToken cancellationToken = default);
    Task<List<ProductPriceHistoryDto>> GetPriceHistoryAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>Creates the product and, if OpeningStock > 0, posts an OpeningStock inventory transaction — both in a single atomic transaction.</summary>
    Task<int> CreateAsync(CreateProductDto dto, int currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Updates product fields. If any of the three prices changed, records a ProductPriceHistory entry — this never happens silently.</summary>
    Task UpdateAsync(UpdateProductDto dto, int currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deactivates (IsActive = false). Products are never physically deleted once they may have transaction history.</summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task ReactivateAsync(int id, CancellationToken cancellationToken = default);
}
