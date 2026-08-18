using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>A return against a previously Posted SalesInvoice. Never deletes or edits the original invoice; increases stock and issues a client credit/refund instead.</summary>
public class SalesReturn : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    public int SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;

    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
}
