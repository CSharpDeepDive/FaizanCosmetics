using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>
/// The single entry point for every stock movement in the system. Sales, purchases, returns,
/// and adjustments all call this instead of touching Product.CurrentStock directly, so every
/// change is guaranteed to produce a matching InventoryTransaction row and respect the
/// AllowNegativeStock setting. Does not manage its own database transaction — callers that need
/// atomicity with other writes (e.g. posting a sales invoice) should wrap this call together
/// with their other repository calls inside IUnitOfWork.ExecuteInTransactionAsync.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Posts one stock movement for a product: validates availability (unless negative stock is
    /// allowed), updates Product.CurrentStock, and inserts the corresponding InventoryTransaction.
    /// Does NOT call SaveChangesAsync — the caller controls when the unit of work is committed.
    /// </summary>
    Task<InventoryTransaction> PostTransactionAsync(
        Product product,
        InventoryTransactionType transactionType,
        decimal quantity,
        decimal unitCost,
        ReferenceType referenceType,
        int referenceId,
        int userId,
        string? notes = null,
        int? batchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>True for transaction types that increase stock (Purchase, SaleReturn, AdjustmentIncrease, OpeningStock); false for types that decrease it (Sale, PurchaseReturn, AdjustmentDecrease, Damage, Theft, Expiry).</summary>
    bool IsIncreaseType(InventoryTransactionType transactionType);
}
