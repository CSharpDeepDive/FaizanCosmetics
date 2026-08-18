using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Suppliers;

public class SupplierPaymentServiceTests
{
    private static (SupplierService SupplierService, SupplierPaymentService PaymentService, SupplierLedgerService LedgerService) CreateServices()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var ledgerService = new SupplierLedgerService(unitOfWork);
        var auditService = new TestAuditService();
        var supplierService = new SupplierService(unitOfWork, ledgerService, auditService);
        var paymentService = new SupplierPaymentService(unitOfWork, ledgerService, auditService);
        return (supplierService, paymentService, ledgerService);
    }

    [Fact]
    public async Task PaySupplierAsync_ReducesWhatWeOwe()
    {
        var (supplierService, paymentService, ledgerService) = CreateServices();

        var supplierId = await supplierService.CreateAsync(new CreateSupplierDto { Name = "Owed Supplier", OpeningBalance = 5000 }, currentUserId: 1);

        await paymentService.PaySupplierAsync(new PaySupplierDto
        {
            SupplierId = supplierId,
            Amount = 2000,
            PaymentMethod = PaymentMethod.Cash
        }, currentUserId: 1);

        var balance = await ledgerService.GetBalanceAsync(supplierId);
        balance.Should().Be(3000m);
    }

    [Fact]
    public async Task PaySupplierAsync_ZeroOrNegativeAmount_ThrowsValidationAppException()
    {
        var (supplierService, paymentService, _) = CreateServices();
        var supplierId = await supplierService.CreateAsync(new CreateSupplierDto { Name = "Supplier" }, currentUserId: 1);

        var act = async () => await paymentService.PaySupplierAsync(new PaySupplierDto
        {
            SupplierId = supplierId,
            Amount = 0,
            PaymentMethod = PaymentMethod.Cash
        }, currentUserId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task PaySupplierAsync_UnknownSupplier_ThrowsValidationAppException()
    {
        var (_, paymentService, _) = CreateServices();

        var act = async () => await paymentService.PaySupplierAsync(new PaySupplierDto
        {
            SupplierId = 99999,
            Amount = 100,
            PaymentMethod = PaymentMethod.Cash
        }, currentUserId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
