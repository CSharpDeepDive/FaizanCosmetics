using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class SupplierPaymentService : ISupplierPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupplierLedgerService _supplierLedgerService;
    private readonly IAuditService _auditService;

    public SupplierPaymentService(IUnitOfWork unitOfWork, ISupplierLedgerService supplierLedgerService, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _supplierLedgerService = supplierLedgerService;
        _auditService = auditService;
    }

    public async Task PaySupplierAsync(PaySupplierDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Amount <= 0)
        {
            throw new ValidationAppException("Payment amount must be greater than zero.");
        }

        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(dto.SupplierId, cancellationToken)
            ?? throw new ValidationAppException("Supplier not found.");

        var payment = new SupplierPayment
        {
            SupplierId = dto.SupplierId,
            PaymentDate = DateTime.UtcNow,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes,
            PaidByUserId = currentUserId
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _unitOfWork.SupplierPayments.Add(payment);
            await _unitOfWork.SaveChangesAsync(ct);

            await _supplierLedgerService.PostEntryAsync(
                dto.SupplierId, SupplierLedgerEntryType.Payment, ReferenceType.SupplierPayment, payment.Id,
                debit: dto.Amount, credit: 0, currentUserId,
                $"Payment made ({dto.PaymentMethod}){(string.IsNullOrWhiteSpace(dto.ReferenceNumber) ? "" : $" — Ref: {dto.ReferenceNumber}")}", ct);
        }, cancellationToken);

        await _auditService.LogAsync(currentUserId, "SupplierPaymentMade", "Supplier", supplier.Id, null, null,
            $"Paid {dto.Amount:N2} to '{supplier.Name}' via {dto.PaymentMethod}.", cancellationToken);
    }
}
