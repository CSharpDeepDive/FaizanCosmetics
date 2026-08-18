using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface IPurchaseInvoiceRepository
{
    Task<PurchaseInvoice?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default);
    Task<string> GenerateNextInvoiceNumberAsync(string prefix, CancellationToken cancellationToken = default);
    Task<(List<PurchaseInvoice> Items, int TotalCount)> SearchAsync(string? invoiceNumber, int? supplierId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    void Add(PurchaseInvoice invoice);
    void Update(PurchaseInvoice invoice);
}
