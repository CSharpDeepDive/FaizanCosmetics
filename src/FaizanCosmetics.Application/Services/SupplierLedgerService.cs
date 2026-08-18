using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class SupplierLedgerService : ISupplierLedgerService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierLedgerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> PostEntryAsync(
        int supplierId,
        SupplierLedgerEntryType entryType,
        ReferenceType referenceType,
        int referenceId,
        decimal debit,
        decimal credit,
        int userId,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (debit < 0 || credit < 0)
        {
            throw new ValidationAppException("Ledger debit and credit amounts cannot be negative.");
        }
        if (debit == 0 && credit == 0)
        {
            throw new ValidationAppException("A ledger entry must have a non-zero debit or credit.");
        }

        var priorBalance = await _unitOfWork.SupplierLedgers.GetBalanceAsync(supplierId, cancellationToken);
        var newBalance = priorBalance + credit - debit; // Credit increases what we owe; Debit decreases it

        _unitOfWork.SupplierLedgers.Add(new SupplierLedgerEntry
        {
            SupplierId = supplierId,
            EntryDate = DateTime.UtcNow,
            EntryType = entryType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Debit = debit,
            Credit = credit,
            Balance = newBalance,
            Description = description,
            UserId = userId
        });

        return newBalance;
    }

    public Task<decimal> GetBalanceAsync(int supplierId, CancellationToken cancellationToken = default) =>
        _unitOfWork.SupplierLedgers.GetBalanceAsync(supplierId, cancellationToken);

    public async Task<SupplierStatementDto> GetStatementAsync(int supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new ValidationAppException("Supplier not found.");

        var openingBalance = fromDate.HasValue
            ? await _unitOfWork.SupplierLedgers.GetBalanceAsOfAsync(supplierId, fromDate.Value, cancellationToken)
            : 0m;

        var entries = await _unitOfWork.SupplierLedgers.GetStatementAsync(supplierId, fromDate, toDate, cancellationToken);

        var runningBalance = openingBalance;
        var entryDtos = new List<SupplierLedgerEntryDto>(entries.Count);
        foreach (var entry in entries)
        {
            runningBalance += entry.Credit - entry.Debit;
            entryDtos.Add(new SupplierLedgerEntryDto
            {
                EntryDate = entry.EntryDate,
                EntryType = entry.EntryType.ToString(),
                Description = entry.Description,
                Debit = entry.Debit,
                Credit = entry.Credit,
                Balance = runningBalance,
                UserName = entry.User?.FullName ?? string.Empty
            });
        }

        return new SupplierStatementDto
        {
            SupplierName = supplier.Name,
            OpeningBalance = openingBalance,
            ClosingBalance = runningBalance,
            Entries = entryDtos
        };
    }
}
