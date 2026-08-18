using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>Mirrors IClientLedgerService exactly in shape; sign convention is reversed (Credit
/// increases what we owe the supplier, Debit decreases it) — see SupplierLedgerEntry's doc comment.</summary>
public interface ISupplierLedgerService
{
    Task<decimal> PostEntryAsync(
        int supplierId,
        SupplierLedgerEntryType entryType,
        ReferenceType referenceType,
        int referenceId,
        decimal debit,
        decimal credit,
        int userId,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<decimal> GetBalanceAsync(int supplierId, CancellationToken cancellationToken = default);
    Task<SupplierStatementDto> GetStatementAsync(int supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}
