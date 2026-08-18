using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Infrastructure.Data;
using FaizanCosmetics.Infrastructure.Repositories;

namespace FaizanCosmetics.Tests.Common;

/// <summary>Wires a real UnitOfWork (real repositories, real EF Core query logic) against an
/// isolated in-memory ApplicationDbContext — so these tests exercise the actual data-access code,
/// not a hand-rolled fake.</summary>
public static class TestUnitOfWorkFactory
{
    public static (ApplicationDbContext Context, IUnitOfWork UnitOfWork) Create()
    {
        var context = TestDbContextFactory.Create();
        IUnitOfWork unitOfWork = new UnitOfWork(
            context,
            new UserRepository(context),
            new ProductRepository(context),
            new ClientRepository(context),
            new ClientLedgerRepository(context),
            new ClientPaymentRepository(context),
            new SupplierRepository(context),
            new SupplierLedgerRepository(context),
            new SupplierPaymentRepository(context),
            new CategoryRepository(context),
            new SalesInvoiceRepository(context),
            new PurchaseInvoiceRepository(context),
            new AppSettingRepository(context));
        return (context, unitOfWork);
    }
}
