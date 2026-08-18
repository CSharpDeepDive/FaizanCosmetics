namespace FaizanCosmetics.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Manager = 2,
    Cashier = 3
}

public enum ClientType
{
    Retail = 1,
    Wholesale = 2
}

public enum InventoryTransactionType
{
    Purchase = 1,
    Sale = 2,
    SaleReturn = 3,
    PurchaseReturn = 4,
    AdjustmentIncrease = 5,
    AdjustmentDecrease = 6,
    Damage = 7,
    Theft = 8,
    Expiry = 9,
    OpeningStock = 10
}

/// <summary>
/// Identifies the source document type that caused an inventory transaction,
/// client ledger entry, or supplier ledger entry. Used together with ReferenceId
/// to trace a stock/financial movement back to its originating document.
/// </summary>
public enum ReferenceType
{
    SalesInvoice = 1,
    SalesReturn = 2,
    PurchaseInvoice = 3,
    PurchaseReturn = 4,
    InventoryAdjustment = 5,
    ClientPayment = 6,
    SupplierPayment = 7,
    OpeningBalance = 8
}

public enum InvoiceStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3
}

public enum PaymentStatus
{
    Paid = 1,
    Partial = 2,
    Pending = 3
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    Mixed = 4
}

public enum ClientLedgerEntryType
{
    OpeningBalance = 1,
    Sale = 2,
    Payment = 3,
    SalesReturn = 4,
    Adjustment = 5,
    CreditNote = 6
}

public enum SupplierLedgerEntryType
{
    OpeningBalance = 1,
    Purchase = 2,
    Payment = 3,
    PurchaseReturn = 4,
    Adjustment = 5
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Sent = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5
}

public enum AdjustmentReason
{
    Damage = 1,
    Theft = 2,
    Expiry = 3,
    StockCorrection = 4,
    OpeningStock = 5,
    Other = 6
}

public enum PriceChangeReason
{
    SupplierPriceUpdate = 1,
    MarketAdjustment = 2,
    Promotion = 3,
    Correction = 4,
    Other = 5
}
