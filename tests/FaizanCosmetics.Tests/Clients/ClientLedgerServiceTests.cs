using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Clients;

public class ClientLedgerServiceTests
{
    private static async Task<(ClientLedgerService Service, int ClientId)> CreateWithClientAsync()
    {
        var (context, unitOfWork) = TestUnitOfWorkFactory.Create();
        var client = new Client { ClientCode = "CL-000001", Name = "Test Client", IsActive = true, ClientType = ClientType.Retail };
        var user = new User { Username = "u1", PasswordHash = "x", FullName = "User One", Role = UserRole.Admin, IsActive = true };
        context.Clients.Add(client);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (new ClientLedgerService(unitOfWork), client.Id);
    }

    [Fact]
    public async Task PostEntryAsync_Debit_IncreasesBalance()
    {
        var (service, clientId) = await CreateWithClientAsync();

        var balance = await service.PostEntryAsync(clientId, ClientLedgerEntryType.Sale, ReferenceType.SalesInvoice, 1, debit: 1500, credit: 0, userId: 1);

        balance.Should().Be(1500m);
    }

    [Fact]
    public async Task PostEntryAsync_SequentialEntries_ComputeCorrectRunningBalance()
    {
        var (service, clientId) = await CreateWithClientAsync();

        await service.PostEntryAsync(clientId, ClientLedgerEntryType.OpeningBalance, ReferenceType.OpeningBalance, clientId, debit: 1000, credit: 0, userId: 1);
        await service.PostEntryAsync(clientId, ClientLedgerEntryType.Sale, ReferenceType.SalesInvoice, 1, debit: 2000, credit: 0, userId: 1);
        var finalBalance = await service.PostEntryAsync(clientId, ClientLedgerEntryType.Payment, ReferenceType.ClientPayment, 1, debit: 0, credit: 1200, userId: 1);

        finalBalance.Should().Be(1800m); // 1000 + 2000 - 1200
    }

    [Fact]
    public async Task PostEntryAsync_ZeroDebitAndCredit_ThrowsValidationAppException()
    {
        var (service, clientId) = await CreateWithClientAsync();

        var act = async () => await service.PostEntryAsync(clientId, ClientLedgerEntryType.Adjustment, ReferenceType.OpeningBalance, clientId, debit: 0, credit: 0, userId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task PostEntryAsync_NegativeAmount_ThrowsValidationAppException()
    {
        var (service, clientId) = await CreateWithClientAsync();

        var act = async () => await service.PostEntryAsync(clientId, ClientLedgerEntryType.Sale, ReferenceType.SalesInvoice, 1, debit: -100, credit: 0, userId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task GetStatementAsync_WithDateFilter_ComputesCorrectOpeningBalance()
    {
        var (service, clientId) = await CreateWithClientAsync();

        await service.PostEntryAsync(clientId, ClientLedgerEntryType.OpeningBalance, ReferenceType.OpeningBalance, clientId, debit: 500, credit: 0, userId: 1);

        // Everything else happens "today" in these tests, so filtering fromDate = tomorrow
        // should show an opening balance equal to everything posted so far, and zero new entries.
        var statement = await service.GetStatementAsync(clientId, DateTime.UtcNow.Date.AddDays(1), null);

        statement.OpeningBalance.Should().Be(500m);
        statement.Entries.Should().BeEmpty();
        statement.ClosingBalance.Should().Be(500m);
    }

    [Fact]
    public async Task GetStatementAsync_WithoutDateFilter_ShowsAllEntriesWithZeroOpeningBalance()
    {
        var (service, clientId) = await CreateWithClientAsync();

        await service.PostEntryAsync(clientId, ClientLedgerEntryType.OpeningBalance, ReferenceType.OpeningBalance, clientId, debit: 500, credit: 0, userId: 1);
        await service.PostEntryAsync(clientId, ClientLedgerEntryType.Payment, ReferenceType.ClientPayment, 1, debit: 0, credit: 200, userId: 1);

        var statement = await service.GetStatementAsync(clientId, null, null);

        statement.OpeningBalance.Should().Be(0m);
        statement.Entries.Should().HaveCount(2);
        statement.ClosingBalance.Should().Be(300m);
        statement.Entries[0].Balance.Should().Be(500m);
        statement.Entries[1].Balance.Should().Be(300m);
    }
}
