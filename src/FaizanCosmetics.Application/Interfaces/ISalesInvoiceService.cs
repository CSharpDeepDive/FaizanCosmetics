using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>
/// Posts sales invoices. Implements spec §9's full posting sequence (validate products/quantities/
/// stock, calculate discounts/tax/subtotal/grand total/paid/due, update inventory, create
/// inventory transactions, create/update the client ledger) as one atomic transaction via
/// IUnitOfWork.ExecuteInTransactionAsync — everything succeeds together or rolls back together.
///
/// Invoice-level discount is applied by distributing it proportionally across each item's
/// subtotal-after-item-discount before tax is calculated, so every stored line (DiscountAmount,
/// TaxAmount, LineTotal) is individually accurate and the lines still sum to the invoice totals —
/// rather than being bolted on as a single flat deduction that would leave per-line figures
/// inconsistent with the invoice total.
/// </summary>
public interface ISalesInvoiceService
{
    Task<int> PostInvoiceAsync(PostSalesInvoiceDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<SalesInvoiceDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(List<SalesInvoiceListItemDto> Items, int TotalCount)> SearchAsync(string? invoiceNumber, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Admin/Manager only (enforced here, not just hidden in the UI). Reverses the invoice's stock and ledger effects and marks it Cancelled — never deletes it, so it remains visible in Sales History as history.</summary>
    Task CancelAsync(int invoiceId, string reason, int currentUserId, CancellationToken cancellationToken = default);
}
