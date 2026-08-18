using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

public interface ISupplierPaymentService
{
    /// <summary>Records a payment we make to a supplier and posts the matching Debit ledger entry (reducing what we owe), atomically. Like IClientPaymentService, always posts as a general on-account payment today — allocation against specific purchase invoices is a later refinement.</summary>
    Task PaySupplierAsync(PaySupplierDto dto, int currentUserId, CancellationToken cancellationToken = default);
}
