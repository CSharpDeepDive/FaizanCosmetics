using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface ISalesInvoiceRepository
{
    Task<SalesInvoice?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default);
    Task<SalesInvoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<string> GenerateNextInvoiceNumberAsync(string prefix, CancellationToken cancellationToken = default);
    Task<(List<SalesInvoice> Items, int TotalCount)> SearchAsync(string? invoiceNumber, int? clientId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    void Add(SalesInvoice invoice);
    void Update(SalesInvoice invoice);
}
