using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class ClientService : IClientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClientLedgerService _clientLedgerService;
    private readonly IAuditService _auditService;

    public ClientService(IUnitOfWork unitOfWork, IClientLedgerService clientLedgerService, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _clientLedgerService = clientLedgerService;
        _auditService = auditService;
    }

    public async Task<(List<ClientListItemDto> Items, int TotalCount)> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (clients, total) = await _unitOfWork.Clients.SearchAsync(searchText, pageNumber, pageSize, cancellationToken);
        var balances = await _unitOfWork.ClientLedgers.GetBalancesAsync(clients.Select(c => c.Id), cancellationToken);

        var items = clients.Select(c => new ClientListItemDto
        {
            Id = c.Id,
            ClientCode = c.ClientCode,
            Name = c.Name,
            Phone = c.Phone,
            ClientType = c.ClientType,
            Balance = balances.GetValueOrDefault(c.Id),
            CreditLimit = c.CreditLimit,
            IsActive = c.IsActive,
            IsWalkInCustomer = c.IsWalkInCustomer
        }).ToList();

        return (items, total);
    }

    public async Task<ClientDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);
        if (client is null) return null;

        var balance = await _clientLedgerService.GetBalanceAsync(id, cancellationToken);

        return new ClientDetailDto
        {
            Id = client.Id,
            ClientCode = client.ClientCode,
            Name = client.Name,
            Phone = client.Phone,
            Address = client.Address,
            Email = client.Email,
            ClientType = client.ClientType,
            CreditLimit = client.CreditLimit,
            Balance = balance,
            IsActive = client.IsActive,
            IsWalkInCustomer = client.IsWalkInCustomer,
            RegistrationDate = client.RegistrationDate
        };
    }

    public async Task<int> CreateAsync(CreateClientDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        Validate(dto.Name, dto.CreditLimit);

        var client = new Client
        {
            ClientCode = await _unitOfWork.Clients.GenerateNextClientCodeAsync(cancellationToken),
            Name = dto.Name.Trim(),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Address = dto.Address,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            ClientType = dto.ClientType,
            CreditLimit = dto.CreditLimit,
            OpeningBalance = dto.OpeningBalance,
            IsActive = true,
            RegistrationDate = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _unitOfWork.Clients.Add(client);
            await _unitOfWork.SaveChangesAsync(ct); // assigns client.Id for the ledger entry FK below

            if (dto.OpeningBalance != 0)
            {
                await _clientLedgerService.PostEntryAsync(
                    client.Id, ClientLedgerEntryType.OpeningBalance, ReferenceType.OpeningBalance, client.Id,
                    debit: dto.OpeningBalance > 0 ? dto.OpeningBalance : 0,
                    credit: dto.OpeningBalance < 0 ? -dto.OpeningBalance : 0,
                    currentUserId, "Opening balance recorded at client creation.", ct);
            }
        }, cancellationToken);

        await _auditService.LogAsync(currentUserId, "ClientCreated", "Client", client.Id, null, null, $"Created client '{client.Name}' ({client.ClientCode}).", cancellationToken);
        return client.Id;
    }

    public async Task UpdateAsync(UpdateClientDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        Validate(dto.Name, dto.CreditLimit);

        var client = await _unitOfWork.Clients.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new ValidationAppException("Client not found.");

        client.Name = dto.Name.Trim();
        client.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        client.Address = dto.Address;
        client.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        client.ClientType = dto.ClientType;
        client.CreditLimit = dto.CreditLimit;

        _unitOfWork.Clients.Update(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, "ClientUpdated", "Client", client.Id, null, null, $"Updated client '{client.Name}'.", cancellationToken);
    }

    public async Task DeactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken)
            ?? throw new ValidationAppException("Client not found.");

        if (client.IsWalkInCustomer)
        {
            throw new ValidationAppException("The Walk-in Customer record cannot be deactivated.");
        }

        client.IsActive = false;
        _unitOfWork.Clients.Update(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, "ClientDeactivated", "Client", client.Id, null, null, $"Deactivated client '{client.Name}'.", cancellationToken);
    }

    public async Task ReactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken)
            ?? throw new ValidationAppException("Client not found.");

        client.IsActive = true;
        _unitOfWork.Clients.Update(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, "ClientReactivated", "Client", client.Id, null, null, $"Reactivated client '{client.Name}'.", cancellationToken);
    }

    private static void Validate(string name, decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationAppException("Client name is required.");
        if (creditLimit < 0) throw new ValidationAppException("Credit limit cannot be negative.");
    }
}
