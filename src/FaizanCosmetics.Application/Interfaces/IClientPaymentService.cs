using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

public interface IClientPaymentService
{
    /// <summary>Records a payment/advance from a client and posts the matching Credit ledger entry, atomically. Not yet allocated against specific invoices — invoice-level allocation is completed in Phase 5 once SalesInvoice exists to allocate against; today this always posts as a general on-account credit.</summary>
    Task ReceivePaymentAsync(ReceiveClientPaymentDto dto, int currentUserId, CancellationToken cancellationToken = default);
}
