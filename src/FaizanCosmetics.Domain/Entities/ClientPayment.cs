using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>A payment received from a client, optionally allocated across one or more outstanding invoices.</summary>
public class ClientPayment : BaseEntity
{
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }

    public int ReceivedByUserId { get; set; }
    public User ReceivedByUser { get; set; } = null!;

    public ICollection<ClientPaymentAllocation> Allocations { get; set; } = new List<ClientPaymentAllocation>();
}
