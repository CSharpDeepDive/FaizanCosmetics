namespace FaizanCosmetics.Application.DTOs;

public class SupplierListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ContactPerson { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}

public class SupplierDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }

    /// <summary>Posted as a Credit ledger entry (amount already owed before using this system) atomically with supplier creation. Zero is valid.</summary>
    public decimal OpeningBalance { get; set; }
}

public class UpdateSupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
}

public class SupplierLedgerEntryDto
{
    public DateTime EntryDate { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class SupplierStatementDto
{
    public string SupplierName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<SupplierLedgerEntryDto> Entries { get; set; } = new();
}

public class PaySupplierDto
{
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }
    public Domain.Enums.PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
