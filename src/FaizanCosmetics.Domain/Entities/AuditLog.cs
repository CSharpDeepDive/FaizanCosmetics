using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>Immutable, append-only record of security- and finance-sensitive actions (login, price change, invoice posting/cancellation, stock adjustment, payment, client/user modification, etc.).</summary>
public class AuditLog : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Short verb/action code, e.g. "Login", "InvoicePosted", "PriceChanged", "StockAdjusted".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Entity type name affected, e.g. "SalesInvoice", "Product".</summary>
    public string Entity { get; set; } = string.Empty;
    public int? EntityId { get; set; }

    public DateTime DateTime { get; set; } = DateTime.UtcNow;

    /// <summary>Serialized (JSON) snapshot of relevant old values, when applicable.</summary>
    public string? OldValue { get; set; }

    /// <summary>Serialized (JSON) snapshot of relevant new values, when applicable.</summary>
    public string? NewValue { get; set; }

    public string? Description { get; set; }
}
