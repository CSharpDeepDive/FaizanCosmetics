namespace FaizanCosmetics.Application.DTOs;

public class PurchaseInvoiceItemInputDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>The negotiated cost for this receipt — may differ from the product's current PurchasePrice. Does NOT retroactively update Product.PurchasePrice; use IProductService.UpdateAsync (which logs price history) if the standing cost should change.</summary>
    public decimal UnitCost { get; set; }
    public decimal DiscountPercent { get; set; }
}

public class PostPurchaseInvoiceDto
{
    public int SupplierId { get; set; }
    public List<PurchaseInvoiceItemInputDto> Items { get; set; } = new();
    public decimal PaidAmount { get; set; }
    public Domain.Enums.PaymentMethod PaymentMethod { get; set; }
    public string? SupplierInvoiceReference { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseInvoiceListItemDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public Domain.Enums.InvoiceStatus Status { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
}

public class PurchaseInvoiceItemDetailDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class PurchaseInvoiceDetailDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public Domain.Enums.InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public List<PurchaseInvoiceItemDetailDto> Items { get; set; } = new();
}
