using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class ClientLedgerService : IClientLedgerService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClientLedgerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> PostEntryAsync(
        int clientId,
        ClientLedgerEntryType entryType,
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

        var priorBalance = await _unitOfWork.ClientLedgers.GetBalanceAsync(clientId, cancellationToken);
        var newBalance = priorBalance + debit - credit;

        _unitOfWork.ClientLedgers.Add(new ClientLedgerEntry
        {
            ClientId = clientId,
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

    public Task<decimal> GetBalanceAsync(int clientId, CancellationToken cancellationToken = default) =>
        _unitOfWork.ClientLedgers.GetBalanceAsync(clientId, cancellationToken);

    public async Task<ClientStatementDto> GetStatementAsync(int clientId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(clientId, cancellationToken)
            ?? throw new ValidationAppException("Client not found.");

        var openingBalance = fromDate.HasValue
            ? await _unitOfWork.ClientLedgers.GetBalanceAsOfAsync(clientId, fromDate.Value, cancellationToken)
            : 0m;

        var entries = await _unitOfWork.ClientLedgers.GetStatementAsync(clientId, fromDate, toDate, cancellationToken);

        var runningBalance = openingBalance;
        var entryDtos = new List<ClientLedgerEntryDto>(entries.Count);
        foreach (var entry in entries)
        {
            runningBalance += entry.Debit - entry.Credit;
            entryDtos.Add(new ClientLedgerEntryDto
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

        return new ClientStatementDto
        {
            ClientName = client.Name,
            OpeningBalance = openingBalance,
            ClosingBalance = runningBalance,
            Entries = entryDtos
        };
    }
}
