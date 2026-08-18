namespace FaizanCosmetics.Application.DTOs;

public class ProductListItemDto
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public bool IsActive { get; set; }
}

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal ReorderLevel { get; set; }
    public string? Description { get; set; }
    public bool HasExpiry { get; set; }
    public bool IsActive { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal ReorderLevel { get; set; }
    public string? Description { get; set; }
    public bool HasExpiry { get; set; }

    /// <summary>Initial stock quantity, posted as an OpeningStock inventory transaction. Zero is valid (product with no stock yet).</summary>
    public decimal OpeningStock { get; set; }
}

public class UpdateProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal ReorderLevel { get; set; }
    public string? Description { get; set; }
    public bool HasExpiry { get; set; }

    /// <summary>Reason recorded in ProductPriceHistory when any of the three prices changed. Ignored if no price changed.</summary>
    public FaizanCosmetics.Domain.Enums.PriceChangeReason PriceChangeReason { get; set; } = FaizanCosmetics.Domain.Enums.PriceChangeReason.Correction;
    public string? PriceChangeNotes { get; set; }
}

public class ProductPriceHistoryDto
{
    public DateTime ChangedDate { get; set; }
    public string ChangedByUserName { get; set; } = string.Empty;
    public decimal OldPurchasePrice { get; set; }
    public decimal NewPurchasePrice { get; set; }
    public decimal OldSellingPrice { get; set; }
    public decimal NewSellingPrice { get; set; }
    public decimal OldWholesalePrice { get; set; }
    public decimal NewWholesalePrice { get; set; }
    public string Reason { get; set; } = string.Empty;
}
