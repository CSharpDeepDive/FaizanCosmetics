using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Clients;

public class ClientServiceTests
{
    private static (ClientService Service, ClientLedgerService LedgerService) CreateServices()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var ledgerService = new ClientLedgerService(unitOfWork);
        var auditService = new TestAuditService();
        var clientService = new ClientService(unitOfWork, ledgerService, auditService);
        return (clientService, ledgerService);
    }

    [Fact]
    public async Task CreateAsync_WithPositiveOpeningBalance_PostsDebitLedgerEntry()
    {
        var (service, ledger) = CreateServices();

        var clientId = await service.CreateAsync(new CreateClientDto
        {
            Name = "Ayesha Traders",
            Phone = "0300-1234567",
            ClientType = ClientType.Wholesale,
            CreditLimit = 50000,
            OpeningBalance = 5000
        }, currentUserId: 1);

        var balance = await ledger.GetBalanceAsync(clientId);
        balance.Should().Be(5000m);

        var client = await service.GetByIdAsync(clientId);
        client!.Balance.Should().Be(5000m);
        client.ClientCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateAsync_WithZeroOpeningBalance_PostsNoLedgerEntry()
    {
        var (service, ledger) = CreateServices();

        var clientId = await service.CreateAsync(new CreateClientDto
        {
            Name = "Retail Walk-in Style Client",
            ClientType = ClientType.Retail,
            OpeningBalance = 0
        }, currentUserId: 1);

        var balance = await ledger.GetBalanceAsync(clientId);
        balance.Should().Be(0m);
    }

    [Fact]
    public async Task CreateAsync_WithNegativeOpeningBalance_PostsCreditEntry_MeaningClientIsInCredit()
    {
        var (service, ledger) = CreateServices();

        var clientId = await service.CreateAsync(new CreateClientDto
        {
            Name = "Prepaid Client",
            ClientType = ClientType.Retail,
            OpeningBalance = -1000 // client has a 1000 credit/advance
        }, currentUserId: 1);

        var balance = await ledger.GetBalanceAsync(clientId);
        balance.Should().Be(-1000m);
    }

    [Fact]
    public async Task CreateAsync_WithNegativeCreditLimit_ThrowsValidationAppException()
    {
        var (service, _) = CreateServices();

        var act = async () => await service.CreateAsync(new CreateClientDto
        {
            Name = "Bad Client",
            CreditLimit = -100
        }, currentUserId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsValidationAppException()
    {
        var (service, _) = CreateServices();

        var act = async () => await service.CreateAsync(new CreateClientDto { Name = "   " }, currentUserId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeactivateAsync_OnWalkInCustomer_ThrowsValidationAppException()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var walkIn = new FaizanCosmetics.Domain.Entities.Client
        {
            ClientCode = "CL-000000",
            Name = "Walk-in Customer",
            IsWalkInCustomer = true,
            IsActive = true,
            ClientType = ClientType.Retail
        };
        unitOfWork.Clients.Add(walkIn);
        await unitOfWork.SaveChangesAsync();

        var ledgerService = new ClientLedgerService(unitOfWork);
        var service = new ClientService(unitOfWork, ledgerService, new TestAuditService());

        var act = async () => await service.DeactivateAsync(walkIn.Id, currentUserId: 1);
        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeactivateAsync_OnRegularClient_Succeeds_AndCanBeReactivated()
    {
        var (service, _) = CreateServices();

        var clientId = await service.CreateAsync(new CreateClientDto { Name = "Temp Client" }, currentUserId: 1);

        await service.DeactivateAsync(clientId, currentUserId: 1);
        var deactivated = await service.GetByIdAsync(clientId);
        deactivated!.IsActive.Should().BeFalse();

        await service.ReactivateAsync(clientId, currentUserId: 1);
        var reactivated = await service.GetByIdAsync(clientId);
        reactivated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ReturnsCorrectBalancePerClient_WithoutNPlusOneLogicErrors()
    {
        var (service, _) = CreateServices();

        var idA = await service.CreateAsync(new CreateClientDto { Name = "Client A", OpeningBalance = 1000 }, currentUserId: 1);
        var idB = await service.CreateAsync(new CreateClientDto { Name = "Client B", OpeningBalance = 2500 }, currentUserId: 1);

        var (items, total) = await service.SearchAsync(null, 1, 20);

        total.Should().BeGreaterOrEqualTo(2);
        items.First(c => c.Id == idA).Balance.Should().Be(1000m);
        items.First(c => c.Id == idB).Balance.Should().Be(2500m);
    }
}
