using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Suppliers;

public class SupplierLedgerServiceTests
{
    private static async Task<(SupplierLedgerService Service, int SupplierId)> CreateWithSupplierAsync()
    {
        var (context, unitOfWork) = TestUnitOfWorkFactory.Create();
        var supplier = new Supplier { Name = "Test Supplier", IsActive = true };
        var user = new User { Username = "u1", PasswordHash = "x", FullName = "User One", Role = UserRole.Admin, IsActive = true };
        context.Suppliers.Add(supplier);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (new SupplierLedgerService(unitOfWork), supplier.Id);
    }

    [Fact]
    public async Task PostEntryAsync_Credit_IncreasesWhatWeOwe()
    {
        var (service, supplierId) = await CreateWithSupplierAsync();

        var balance = await service.PostEntryAsync(supplierId, SupplierLedgerEntryType.Purchase, ReferenceType.PurchaseInvoice, 1, debit: 0, credit: 2000, userId: 1);

        balance.Should().Be(2000m);
    }

    [Fact]
    public async Task PostEntryAsync_DebitPayment_DecreasesWhatWeOwe()
    {
        var (service, supplierId) = await CreateWithSupplierAsync();

        await service.PostEntryAsync(supplierId, SupplierLedgerEntryType.Purchase, ReferenceType.PurchaseInvoice, 1, debit: 0, credit: 2000, userId: 1);
        var finalBalance = await service.PostEntryAsync(supplierId, SupplierLedgerEntryType.Payment, ReferenceType.SupplierPayment, 1, debit: 800, credit: 0, userId: 1);

        finalBalance.Should().Be(1200m);
    }

    [Fact]
    public async Task PostEntryAsync_ZeroDebitAndCredit_ThrowsValidationAppException()
    {
        var (service, supplierId) = await CreateWithSupplierAsync();

        var act = async () => await service.PostEntryAsync(supplierId, SupplierLedgerEntryType.Adjustment, ReferenceType.OpeningBalance, supplierId, debit: 0, credit: 0, userId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task GetStatementAsync_ComputesRunningBalanceInCreditMinusDebitDirection()
    {
        var (service, supplierId) = await CreateWithSupplierAsync();

        await service.PostEntryAsync(supplierId, SupplierLedgerEntryType.OpeningBalance, ReferenceType.OpeningBalance, supplierId, debit: 0, credit: 500, userId: 1);
        await service.PostEntryAsync(supplierId, SupplierLedgerEntryType.Payment, ReferenceType.SupplierPayment, 1, debit: 200, credit: 0, userId: 1);

        var statement = await service.GetStatementAsync(supplierId, null, null);

        statement.OpeningBalance.Should().Be(0m);
        statement.Entries.Should().HaveCount(2);
        statement.Entries[0].Balance.Should().Be(500m);
        statement.Entries[1].Balance.Should().Be(300m);
        statement.ClosingBalance.Should().Be(300m);
    }
}
