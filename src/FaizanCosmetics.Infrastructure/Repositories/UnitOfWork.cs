using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(
        ApplicationDbContext context,
        IUserRepository users,
        IProductRepository products,
        IClientRepository clients,
        IClientLedgerRepository clientLedgers,
        IClientPaymentRepository clientPayments,
        ISupplierRepository suppliers,
        ISupplierLedgerRepository supplierLedgers,
        ISupplierPaymentRepository supplierPayments,
        ICategoryRepository categories,
        ISalesInvoiceRepository salesInvoices,
        IPurchaseInvoiceRepository purchaseInvoices,
        IAppSettingRepository appSettings)
    {
        _context = context;
        Users = users;
        Products = products;
        Clients = clients;
        ClientLedgers = clientLedgers;
        ClientPayments = clientPayments;
        Suppliers = suppliers;
        SupplierLedgers = supplierLedgers;
        SupplierPayments = supplierPayments;
        Categories = categories;
        SalesInvoices = salesInvoices;
        PurchaseInvoices = purchaseInvoices;
        AppSettings = appSettings;
    }

    public IUserRepository Users { get; }
    public IProductRepository Products { get; }
    public IClientRepository Clients { get; }
    public IClientLedgerRepository ClientLedgers { get; }
    public IClientPaymentRepository ClientPayments { get; }
    public ISupplierRepository Suppliers { get; }
    public ISupplierLedgerRepository SupplierLedgers { get; }
    public ISupplierPaymentRepository SupplierPayments { get; }
    public ICategoryRepository Categories { get; }
    public ISalesInvoiceRepository SalesInvoices { get; }
    public IPurchaseInvoiceRepository PurchaseInvoices { get; }
    public IAppSettingRepository AppSettings { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        // The connection string enables SQL Server's automatic retry-on-transient-failure
        // strategy (see Infrastructure.DependencyInjection). That strategy is only safe with
        // manually-managed transactions if the ENTIRE begin/do-work/commit sequence is wrapped
        // in CreateExecutionStrategy().ExecuteAsync — otherwise a retry could silently attempt
        // to resume an already-open transaction, which SQL Server (and EF Core) explicitly
        // disallow. This is the fix for the "SqlServerRetryingExecutionStrategy does not support
        // user-initiated transactions" error.
        //
        // We call the low-level ExecuteAsync<TState, TResult> overload explicitly (passing
        // `operation` itself as the TState and a static lambda as the delegate) rather than one
        // of IExecutionStrategy's simpler convenience overloads, because those convenience
        // overloads are extension methods and can fail to resolve unambiguously depending on
        // the exact EF Core package version — the 4-parameter instance-interface overload used
        // here is guaranteed present on IExecutionStrategy itself.
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(
            operation,
            static async (dbContext, op, ct) =>
            {
                IDbContextTransaction? transaction = null;
                try
                {
                    transaction = await dbContext.Database.BeginTransactionAsync(ct);
                }
                catch (InvalidOperationException)
                {
                    // The current provider doesn't support relational transactions at all (EF
                    // Core's InMemory provider, used by the test project). Proceeding with
                    // transaction == null means every write below still lands via
                    // SaveChangesAsync, just without a real database transaction wrapping it —
                    // correct for a provider where each SaveChangesAsync call is already atomic
                    // on its own. Production always runs against SQL Server, where this branch
                    // is never taken.
                }

                try
                {
                    await op(ct);
                    await dbContext.SaveChangesAsync(ct);

                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(ct);
                    }
                }
                catch
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(ct);
                    }
                    throw;
                }
                finally
                {
                    if (transaction is not null)
                    {
                        await transaction.DisposeAsync();
                    }
                }

                return true; // dummy TResult — this overload requires one, but callers of ExecuteInTransactionAsync don't need a result back
            },
            verifySucceeded: null,
            cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
