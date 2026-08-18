using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>
/// Single-row table holding runtime-editable business configuration (Settings screen).
/// Distinct from appsettings.json, which holds deployment-level config (connection string,
/// logging). This table holds things a Manager/Admin can change from within the app.
/// </summary>
public class AppSetting : BaseEntity
{
    // Store
    public string StoreName { get; set; } = "Faizan Cosmetics";
    public string? StoreAddress { get; set; }
    public string? StorePhone { get; set; }
    public string? StoreEmail { get; set; }
    public string? LogoPath { get; set; }

    // Sales
    public string InvoicePrefix { get; set; } = "INV";
    public string PurchaseInvoicePrefix { get; set; } = "PINV";
    public string SalesReturnPrefix { get; set; } = "SRTN";
    public string PurchaseReturnPrefix { get; set; } = "PRTN";
    public decimal DefaultDiscountPercent { get; set; }
    public bool TaxEnabled { get; set; }
    public decimal DefaultTaxPercent { get; set; }
    public bool TaxInclusivePricing { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool RequireClientForCreditSale { get; set; } = true;

    // Currency / localization
    public string CurrencyCode { get; set; } = "PKR";
    public string CurrencySymbol { get; set; } = "Rs.";
    public int DecimalPlaces { get; set; } = 2;

    // Printer
    public string? ReceiptPrinterName { get; set; }
    public string? A4PrinterName { get; set; }

    /// <summary>"58mm", "80mm", or "A4".</summary>
    public string DefaultReceiptSize { get; set; } = "80mm";

    // Backup
    public string? BackupDirectory { get; set; }
    public bool AutoBackupEnabled { get; set; }
    public int AutoBackupIntervalHours { get; set; } = 24;

    // Security
    public int SessionTimeoutMinutes { get; set; } = 60;
    public int MinimumPasswordLength { get; set; } = 8;
}
