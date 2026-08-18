using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class ClientPaymentService : IClientPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClientLedgerService _clientLedgerService;
    private readonly IAuditService _auditService;

    public ClientPaymentService(IUnitOfWork unitOfWork, IClientLedgerService clientLedgerService, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _clientLedgerService = clientLedgerService;
        _auditService = auditService;
    }

    public async Task ReceivePaymentAsync(ReceiveClientPaymentDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Amount <= 0)
        {
            throw new ValidationAppException("Payment amount must be greater than zero.");
        }

        var client = await _unitOfWork.Clients.GetByIdAsync(dto.ClientId, cancellationToken)
            ?? throw new ValidationAppException("Client not found.");

        var payment = new ClientPayment
        {
            ClientId = dto.ClientId,
            PaymentDate = DateTime.UtcNow,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes,
            ReceivedByUserId = currentUserId
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _unitOfWork.ClientPayments.Add(payment);
            await _unitOfWork.SaveChangesAsync(ct); // assigns payment.Id for the ledger reference below

            // Not yet allocated against a specific invoice — see IClientPaymentService's doc
            // comment. This always posts as a Credit against the client's general balance,
            // which is correct both for "pay down what you owe" and "pay in advance" (the
            // latter simply drives the balance negative, i.e. into credit).
            await _clientLedgerService.PostEntryAsync(
                dto.ClientId, ClientLedgerEntryType.Payment, ReferenceType.ClientPayment, payment.Id,
                debit: 0, credit: dto.Amount, currentUserId,
                $"Payment received ({dto.PaymentMethod}){(string.IsNullOrWhiteSpace(dto.ReferenceNumber) ? "" : $" — Ref: {dto.ReferenceNumber}")}", ct);
        }, cancellationToken);

        await _auditService.LogAsync(currentUserId, "ClientPaymentReceived", "Client", client.Id, null, null,
            $"Received {dto.Amount:N2} from '{client.Name}' via {dto.PaymentMethod}.", cancellationToken);
    }
}
