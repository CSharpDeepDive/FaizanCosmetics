using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.DTOs;

public class SalesInvoiceItemInputDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>0–100. Validated against nothing today (see IClientService's sibling note on discount permission enforcement being deferred to a later phase) — any non-negative value up to 100 is accepted.</summary>
    public decimal DiscountPercent { get; set; }
}

public class PostSalesInvoiceDto
{
    /// <summary>Null means the system Walk-in Customer.</summary>
    public int? ClientId { get; set; }
    public List<SalesInvoiceItemInputDto> Items { get; set; } = new();

    /// <summary>0–100, applied proportionally across item subtotals before tax — see ISalesInvoiceService's remarks.</summary>
    public decimal InvoiceDiscountPercent { get; set; }

    public decimal PaidAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class SalesInvoiceListItemDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public InvoiceStatus Status { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
}

public class SalesInvoiceItemDetailDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class SalesInvoiceDetailDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public string? CancellationReason { get; set; }
    public List<SalesInvoiceItemDetailDto> Items { get; set; } = new();
}
