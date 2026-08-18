using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class SalesInvoiceService : ISalesInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly IClientLedgerService _clientLedgerService;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public SalesInvoiceService(
        IUnitOfWork unitOfWork,
        IInventoryService inventoryService,
        IClientLedgerService clientLedgerService,
        ITaxCalculationService taxCalculationService,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _clientLedgerService = clientLedgerService;
        _taxCalculationService = taxCalculationService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    private class LineWork
    {
        public Product Product { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineSubtotal { get; set; }
        public decimal ItemDiscountAmount { get; set; }
        public decimal NetAfterItemDiscount { get; set; }
        public decimal InvoiceDiscountShare { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public async Task<int> PostInvoiceAsync(PostSalesInvoiceDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Items.Count == 0)
        {
            throw new ValidationAppException("An invoice must have at least one item.");
        }
        if (dto.InvoiceDiscountPercent is < 0 or > 100)
        {
            throw new ValidationAppException("Invoice discount must be between 0 and 100 percent.");
        }

        var client = dto.ClientId.HasValue
            ? await _unitOfWork.Clients.GetByIdAsync(dto.ClientId.Value, cancellationToken) ?? throw new ValidationAppException("Client not found.")
            : await _unitOfWork.Clients.GetWalkInCustomerAsync(cancellationToken);

        var settings = await _unitOfWork.AppSettings.GetAsync(cancellationToken);

        // Pass 1: per-item figures before any invoice-level discount is distributed.
        var lines = new List<LineWork>(dto.Items.Count);
        foreach (var input in dto.Items)
        {
            if (input.Quantity <= 0)
            {
                throw new ValidationAppException("Every item quantity must be greater than zero.");
            }
            if (input.DiscountPercent is < 0 or > 100)
            {
                throw new ValidationAppException("Item discount must be between 0 and 100 percent.");
            }

            var product = await _unitOfWork.Products.GetByIdAsync(input.ProductId, cancellationToken)
                ?? throw new ValidationAppException($"Product #{input.ProductId} was not found.");
            if (!product.IsActive)
            {
                throw new ValidationAppException($"'{product.Name}' is inactive and cannot be sold.");
            }

            var lineSubtotal = Math.Round(input.Quantity * product.SellingPrice, 2);
            var itemDiscountAmount = Math.Round(lineSubtotal * input.DiscountPercent / 100m, 2);

            lines.Add(new LineWork
            {
                Product = product,
                Quantity = input.Quantity,
                DiscountPercent = input.DiscountPercent,
                UnitPrice = product.SellingPrice,
                LineSubtotal = lineSubtotal,
                ItemDiscountAmount = itemDiscountAmount,
                NetAfterItemDiscount = lineSubtotal - itemDiscountAmount
            });
        }

        // Pass 2: distribute the invoice-level discount proportionally across lines (by their
        // share of the post-item-discount subtotal), then run every line through the one
        // centralized tax calculation so nothing duplicates that math.
        var netAfterItemDiscountTotal = lines.Sum(l => l.NetAfterItemDiscount);
        var invoiceDiscountAmount = netAfterItemDiscountTotal > 0
            ? Math.Round(netAfterItemDiscountTotal * dto.InvoiceDiscountPercent / 100m, 2)
            : 0m;

        var allocatedSoFar = 0m;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            decimal share;
            if (i == lines.Count - 1)
            {
                // Last line absorbs any rounding remainder so the shares sum exactly to invoiceDiscountAmount.
                share = invoiceDiscountAmount - allocatedSoFar;
            }
            else
            {
                share = netAfterItemDiscountTotal > 0
                    ? Math.Round(invoiceDiscountAmount * (line.NetAfterItemDiscount / netAfterItemDiscountTotal), 2)
                    : 0m;
            }
            allocatedSoFar += share;
            line.InvoiceDiscountShare = share;

            var taxableBase = line.NetAfterItemDiscount - share;
            var tax = _taxCalculationService.Calculate(taxableBase, settings.TaxEnabled, settings.DefaultTaxPercent, settings.TaxInclusivePricing);
            line.TaxAmount = tax.TaxAmount;
            line.LineTotal = tax.TotalAmount;
        }

        var subTotal = lines.Sum(l => l.LineSubtotal);
        var totalDiscount = lines.Sum(l => l.ItemDiscountAmount + l.InvoiceDiscountShare);
        var totalTax = lines.Sum(l => l.TaxAmount);
        var grandTotal = lines.Sum(l => l.LineTotal);

        if (dto.PaidAmount < 0)
        {
            throw new ValidationAppException("Paid amount cannot be negative.");
        }
        if (dto.PaidAmount > grandTotal)
        {
            throw new PaymentExceedsDueException();
        }

        var dueAmount = grandTotal - dto.PaidAmount;

        if (dueAmount > 0 && client.IsWalkInCustomer)
        {
            throw new ValidationAppException("Walk-in customers cannot have a due balance. Select a registered client for credit sales, or collect full payment.");
        }

        if (dueAmount > 0 && !client.IsWalkInCustomer)
        {
            var currentOutstanding = await _unitOfWork.ClientLedgers.GetBalanceAsync(client.Id, cancellationToken);
            var wouldBeOutstanding = currentOutstanding + dueAmount;
            if (wouldBeOutstanding > client.CreditLimit && !_currentUser.CanOverrideCreditLimit)
            {
                throw new CreditLimitExceededException(client.Name, client.CreditLimit, wouldBeOutstanding);
            }
        }

        var paymentStatus = dueAmount <= 0 ? PaymentStatus.Paid : (dto.PaidAmount > 0 ? PaymentStatus.Partial : PaymentStatus.Pending);
        var invoiceNumber = await _unitOfWork.SalesInvoices.GenerateNextInvoiceNumberAsync(settings.InvoicePrefix, cancellationToken);

        var invoice = new SalesInvoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.UtcNow,
            ClientId = client.Id,
            SubTotal = subTotal,
            DiscountAmount = totalDiscount,
            TaxAmount = totalTax,
            GrandTotal = grandTotal,
            PaidAmount = dto.PaidAmount,
            DueAmount = dueAmount,
            PaymentStatus = paymentStatus,
            PaymentMethod = dto.PaymentMethod,
            Status = InvoiceStatus.Posted,
            Notes = dto.Notes,
            CreatedByUserId = currentUserId
        };

        foreach (var line in lines)
        {
            invoice.Items.Add(new SalesInvoiceItem
            {
                ProductId = line.Product.Id,
                ProductNameSnapshot = line.Product.Name,
                BarcodeSnapshot = line.Product.Barcode,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                UnitCostSnapshot = line.Product.PurchasePrice,
                DiscountPercent = line.DiscountPercent,
                DiscountAmount = line.ItemDiscountAmount + line.InvoiceDiscountShare,
                TaxPercent = settings.TaxEnabled ? settings.DefaultTaxPercent : 0,
                TaxAmount = line.TaxAmount,
                LineTotal = line.LineTotal
            });
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _unitOfWork.SalesInvoices.Add(invoice);
            await _unitOfWork.SaveChangesAsync(ct); // assigns invoice.Id for the references below

            foreach (var line in lines)
            {
                await _inventoryService.PostTransactionAsync(
                    line.Product, InventoryTransactionType.Sale, line.Quantity, line.Product.PurchasePrice,
                    ReferenceType.SalesInvoice, invoice.Id, currentUserId,
                    $"Sale — Invoice {invoiceNumber}", cancellationToken: ct);
            }

            if (dueAmount > 0)
            {
                await _clientLedgerService.PostEntryAsync(
                    client.Id, ClientLedgerEntryType.Sale, ReferenceType.SalesInvoice, invoice.Id,
                    debit: dueAmount, credit: 0, currentUserId,
                    $"Credit sale — Invoice {invoiceNumber}", ct);
            }
        }, cancellationToken);

        await _auditService.LogAsync(currentUserId, "InvoicePosted", "SalesInvoice", invoice.Id, null, null,
            $"Posted invoice {invoiceNumber} for {client.Name} — Total {grandTotal:N2}, Due {dueAmount:N2}.", cancellationToken);

        return invoice.Id;
    }

    public async Task<SalesInvoiceDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await _unitOfWork.SalesInvoices.GetByIdWithItemsAsync(id, cancellationToken);
        if (invoice is null) return null;

        return new SalesInvoiceDetailDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            ClientName = invoice.Client?.Name ?? "Walk-in Customer",
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = invoice.PaidAmount,
            DueAmount = invoice.DueAmount,
            PaymentStatus = invoice.PaymentStatus,
            PaymentMethod = invoice.PaymentMethod,
            Status = invoice.Status,
            Notes = invoice.Notes,
            CreatedByUserName = invoice.CreatedByUser?.FullName ?? string.Empty,
            CancellationReason = invoice.CancellationReason,
            Items = invoice.Items.Select(i => new SalesInvoiceItemDetailDto
            {
                ProductName = i.ProductNameSnapshot,
                Barcode = i.BarcodeSnapshot,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountPercent = i.DiscountPercent,
                DiscountAmount = i.DiscountAmount,
                TaxAmount = i.TaxAmount,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }

    public async Task<(List<SalesInvoiceListItemDto> Items, int TotalCount)> SearchAsync(string? invoiceNumber, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (invoices, total) = await _unitOfWork.SalesInvoices.SearchAsync(invoiceNumber, null, fromDate, toDate, pageNumber, pageSize, cancellationToken);

        var items = invoices.Select(i => new SalesInvoiceListItemDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            ClientName = i.Client?.Name ?? "Walk-in Customer",
            GrandTotal = i.GrandTotal,
            PaidAmount = i.PaidAmount,
            DueAmount = i.DueAmount,
            PaymentStatus = i.PaymentStatus,
            Status = i.Status,
            CreatedByUserName = i.CreatedByUser?.FullName ?? string.Empty
        }).ToList();

        return (items, total);
    }

    public async Task CancelAsync(int invoiceId, string reason, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Role is not (UserRole.Admin or UserRole.Manager))
        {
            throw new ValidationAppException("Only an Admin or Manager can cancel a posted invoice.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationAppException("A cancellation reason is required.");
        }

        var invoice = await _unitOfWork.SalesInvoices.GetByIdWithItemsAsync(invoiceId, cancellationToken)
            ?? throw new ValidationAppException("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Posted)
        {
            throw new ValidationAppException($"Only a Posted invoice can be cancelled (this one is {invoice.Status}).");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var item in invoice.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, ct);
                if (product is null) continue; // defensive — a product should never be hard-deleted, but never let a missing lookup crash a cancellation

                await _inventoryService.PostTransactionAsync(
                    product, InventoryTransactionType.SaleReturn, item.Quantity, item.UnitCostSnapshot,
                    ReferenceType.SalesInvoice, invoice.Id, currentUserId,
                    $"Cancellation reversal — Invoice {invoice.InvoiceNumber}", cancellationToken: ct);
            }

            if (invoice.DueAmount > 0 && invoice.ClientId.HasValue)
            {
                await _clientLedgerService.PostEntryAsync(
                    invoice.ClientId.Value, ClientLedgerEntryType.Adjustment, ReferenceType.SalesInvoice, invoice.Id,
                    debit: 0, credit: invoice.DueAmount, currentUserId,
                    $"Cancellation reversal — Invoice {invoice.InvoiceNumber}", ct);
            }

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.CancellationReason = reason;
            invoice.CancelledByUserId = currentUserId;
            invoice.CancelledDate = DateTime.UtcNow;
            _unitOfWork.SalesInvoices.Update(invoice);
        }, cancellationToken);

        await _auditService.LogAsync(currentUserId, "InvoiceCancelled", "SalesInvoice", invoice.Id, null, null,
            $"Cancelled invoice {invoice.InvoiceNumber}. Reason: {reason}", cancellationToken);
    }
}
