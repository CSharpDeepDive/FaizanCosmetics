using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Suppliers;

public class SupplierServiceTests
{
    private static (SupplierService Service, SupplierLedgerService LedgerService) CreateServices()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var ledgerService = new SupplierLedgerService(unitOfWork);
        var service = new SupplierService(unitOfWork, ledgerService, new TestAuditService());
        return (service, ledgerService);
    }

    [Fact]
    public async Task CreateAsync_WithPositiveOpeningBalance_PostsCreditLedgerEntry()
    {
        var (service, ledger) = CreateServices();

        var supplierId = await service.CreateAsync(new CreateSupplierDto
        {
            Name = "Beauty Supplies Co",
            Phone = "021-1234567",
            OpeningBalance = 3000
        }, currentUserId: 1);

        var balance = await ledger.GetBalanceAsync(supplierId);
        balance.Should().Be(3000m);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsValidationAppException()
    {
        var (service, _) = CreateServices();

        var act = async () => await service.CreateAsync(new CreateSupplierDto { Name = "   " }, currentUserId: 1);
        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeactivateAsync_ThenReactivate_RoundTrips()
    {
        var (service, _) = CreateServices();
        var supplierId = await service.CreateAsync(new CreateSupplierDto { Name = "Temp Supplier" }, currentUserId: 1);

        await service.DeactivateAsync(supplierId, currentUserId: 1);
        (await service.GetByIdAsync(supplierId))!.IsActive.Should().BeFalse();

        await service.ReactivateAsync(supplierId, currentUserId: 1);
        (await service.GetByIdAsync(supplierId))!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ReturnsCorrectBalancePerSupplier()
    {
        var (service, _) = CreateServices();

        var idA = await service.CreateAsync(new CreateSupplierDto { Name = "Supplier A", OpeningBalance = 1500 }, currentUserId: 1);
        var idB = await service.CreateAsync(new CreateSupplierDto { Name = "Supplier B", OpeningBalance = 750 }, currentUserId: 1);

        var (items, total) = await service.SearchAsync(null, 1, 20);

        total.Should().BeGreaterOrEqualTo(2);
        items.First(s => s.Id == idA).Balance.Should().Be(1500m);
        items.First(s => s.Id == idB).Balance.Should().Be(750m);
    }
}
