using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface ISupplierPaymentRepository
{
    Task<(List<SupplierPayment> Items, int TotalCount)> GetBySupplierAsync(int supplierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    void Add(SupplierPayment payment);
}
