using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Clients;

public class ClientPaymentServiceTests
{
    private static (ClientService ClientService, ClientPaymentService PaymentService, ClientLedgerService LedgerService) CreateServices()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var ledgerService = new ClientLedgerService(unitOfWork);
        var auditService = new TestAuditService();
        var clientService = new ClientService(unitOfWork, ledgerService, auditService);
        var paymentService = new ClientPaymentService(unitOfWork, ledgerService, auditService);
        return (clientService, paymentService, ledgerService);
    }

    [Fact]
    public async Task ReceivePaymentAsync_ReducesOutstandingBalance()
    {
        var (clientService, paymentService, ledgerService) = CreateServices();

        var clientId = await clientService.CreateAsync(new CreateClientDto { Name = "Debtor Client", OpeningBalance = 5000 }, currentUserId: 1);

        await paymentService.ReceivePaymentAsync(new ReceiveClientPaymentDto
        {
            ClientId = clientId,
            Amount = 2000,
            PaymentMethod = PaymentMethod.Cash
        }, currentUserId: 1);

        var balance = await ledgerService.GetBalanceAsync(clientId);
        balance.Should().Be(3000m);
    }

    [Fact]
    public async Task ReceivePaymentAsync_AdvancePayment_MakesBalanceNegative()
    {
        var (clientService, paymentService, ledgerService) = CreateServices();

        var clientId = await clientService.CreateAsync(new CreateClientDto { Name = "Prepay Client" }, currentUserId: 1);

        await paymentService.ReceivePaymentAsync(new ReceiveClientPaymentDto
        {
            ClientId = clientId,
            Amount = 1000,
            PaymentMethod = PaymentMethod.BankTransfer
        }, currentUserId: 1);

        var balance = await ledgerService.GetBalanceAsync(clientId);
        balance.Should().Be(-1000m, "an advance payment with no prior balance puts the client in credit");
    }

    [Fact]
    public async Task ReceivePaymentAsync_ZeroOrNegativeAmount_ThrowsValidationAppException()
    {
        var (clientService, paymentService, _) = CreateServices();
        var clientId = await clientService.CreateAsync(new CreateClientDto { Name = "Client" }, currentUserId: 1);

        var act = async () => await paymentService.ReceivePaymentAsync(new ReceiveClientPaymentDto
        {
            ClientId = clientId,
            Amount = 0,
            PaymentMethod = PaymentMethod.Cash
        }, currentUserId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task ReceivePaymentAsync_UnknownClient_ThrowsValidationAppException()
    {
        var (_, paymentService, _) = CreateServices();

        var act = async () => await paymentService.ReceivePaymentAsync(new ReceiveClientPaymentDto
        {
            ClientId = 99999,
            Amount = 100,
            PaymentMethod = PaymentMethod.Cash
        }, currentUserId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
