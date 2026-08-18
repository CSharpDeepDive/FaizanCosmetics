namespace FaizanCosmetics.Application.Interfaces;

/// <summary>
/// Coordinates a single database transaction across multiple repositories, so that (for example)
/// a sales invoice posting, its inventory transactions, and its client ledger entry either all
/// commit together or all roll back together.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IProductRepository Products { get; }
    IClientRepository Clients { get; }
    IClientLedgerRepository ClientLedgers { get; }
    IClientPaymentRepository ClientPayments { get; }
    ISupplierRepository Suppliers { get; }
    ISupplierLedgerRepository SupplierLedgers { get; }
    ISupplierPaymentRepository SupplierPayments { get; }
    ICategoryRepository Categories { get; }
    ISalesInvoiceRepository SalesInvoices { get; }
    IPurchaseInvoiceRepository PurchaseInvoices { get; }
    IAppSettingRepository AppSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> and every pending change inside one atomic database
    /// transaction, safely wrapped so it's compatible with SQL Server's automatic retry-on-
    /// transient-failure strategy (EnableRetryOnFailure). Use this instead of ad-hoc
    /// SaveChangesAsync calls whenever a multi-step write (e.g. a product plus its opening-stock
    /// inventory transaction, or a sales invoice plus its inventory and ledger effects) must
    /// either all succeed or all roll back together.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
