using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>
/// Posts purchase invoices (direct entry — this phase does not implement the PurchaseOrder →
/// receive workflow; see Phase5-Handover.md §9's documented decision). Mirrors
/// ISalesInvoiceService's atomic posting pattern: validate → calculate item discount/tax via the
/// same centralized ITaxCalculationService → update stock via IInventoryService → update the
/// supplier ledger via ISupplierLedgerService — all inside one ExecuteInTransactionAsync.
/// Does not create ProductBatch rows even for HasExpiry products — batch entry UI is deferred to
/// a later phase; only quantity/cost is tracked here.
/// </summary>
public interface IPurchaseInvoiceService
{
    Task<int> PostInvoiceAsync(PostPurchaseInvoiceDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(List<PurchaseInvoiceListItemDto> Items, int TotalCount)> SearchAsync(string? invoiceNumber, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
