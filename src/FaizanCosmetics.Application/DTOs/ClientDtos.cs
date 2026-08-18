using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.DTOs;

public class ClientListItemDto
{
    public int Id { get; set; }
    public string ClientCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public ClientType ClientType { get; set; }
    public decimal Balance { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; }
    public bool IsWalkInCustomer { get; set; }
}

public class ClientDetailDto
{
    public int Id { get; set; }
    public string ClientCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public ClientType ClientType { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public bool IsWalkInCustomer { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class CreateClientDto
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public ClientType ClientType { get; set; } = ClientType.Retail;
    public decimal CreditLimit { get; set; }

    /// <summary>Posted as an OpeningBalance ledger entry (Debit if positive — the client already owed this before using the system) atomically with client creation. Zero is valid.</summary>
    public decimal OpeningBalance { get; set; }
}

public class UpdateClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public ClientType ClientType { get; set; }
    public decimal CreditLimit { get; set; }
}

public class ClientLedgerEntryDto
{
    public DateTime EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class ClientStatementDto
{
    public string ClientName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<ClientLedgerEntryDto> Entries { get; set; } = new();
}

public class ReceiveClientPaymentDto
{
    public int ClientId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
