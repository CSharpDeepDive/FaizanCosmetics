using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class PurchaseInvoiceService : IPurchaseInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly ISupplierLedgerService _supplierLedgerService;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly IAuditService _auditService;

    public PurchaseInvoiceService(
        IUnitOfWork unitOfWork,
        IInventoryService inventoryService,
        ISupplierLedgerService supplierLedgerService,
        ITaxCalculationService taxCalculationService,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _supplierLedgerService = supplierLedgerService;
        _taxCalculationService = taxCalculationService;
        _auditService = auditService;
    }

    private class LineWork
    {
        public Product Product { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public async Task<int> PostInvoiceAsync(PostPurchaseInvoiceDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Items.Count == 0)
        {
            throw new ValidationAppException("A purchase invoice must have at least one item.");
        }

        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(dto.SupplierId, cancellationToken)
            ?? throw new ValidationAppException("Supplier not found.");

        var settings = await _unitOfWork.AppSettings.GetAsync(cancellationToken);

        var lines = new List<LineWork>(dto.Items.Count);
        foreach (var input in dto.Items)
        {
            if (input.Quantity <= 0)
            {
                throw new ValidationAppException("Every item quantity must be greater than zero.");
            }
            if (input.UnitCost < 0)
            {
                throw new ValidationAppException("Unit cost cannot be negative.");
            }
            if (input.DiscountPercent is < 0 or > 100)
            {
                throw new ValidationAppException("Item discount must be between 0 and 100 percent.");
            }

            var product = await _unitOfWork.Products.GetByIdAsync(input.ProductId, cancellationToken)
                ?? throw new ValidationAppException($"Product #{input.ProductId} was not found.");

            var lineSubtotal = Math.Round(input.Quantity * input.UnitCost, 2);
            var discount = Math.Round(lineSubtotal * input.DiscountPercent / 100m, 2);
            var taxable = lineSubtotal - discount;
            var tax = _taxCalculationService.Calculate(taxable, settings.TaxEnabled, settings.DefaultTaxPercent, settings.TaxInclusivePricing);

            lines.Add(new LineWork
            {
                Product = product,
                Quantity = input.Quantity,
                UnitCost = input.UnitCost,
                DiscountAmount = discount,
                TaxAmount = tax.TaxAmount,
                LineTotal = tax.TotalAmount
            });
        }

        var subTotal = lines.Sum(l => Math.Round(l.Quantity * l.UnitCost, 2));
        var totalDiscount = lines.Sum(l => l.DiscountAmount);
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
        var invoiceNumber = await _unitOfWork.PurchaseInvoices.GenerateNextInvoiceNumberAsync(settings.PurchaseInvoicePrefix, cancellationToken);

        var invoice = new PurchaseInvoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.UtcNow,
            SupplierId = supplier.Id,
            SupplierInvoiceReference = dto.SupplierInvoiceReference,
            SubTotal = subTotal,
            DiscountAmount = totalDiscount,
            TaxAmount = totalTax,
            GrandTotal = grandTotal,
            PaidAmount = dto.PaidAmount,
            DueAmount = dueAmount,
            Status = InvoiceStatus.Posted,
            Notes = dto.Notes,
            CreatedByUserId = currentUserId
        };

        foreach (var line in lines)
        {
            invoice.Items.Add(new PurchaseInvoiceItem
            {
                ProductId = line.Product.Id,
                ProductNameSnapshot = line.Product.Name,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                DiscountAmount = line.DiscountAmount,
                TaxAmount = line.TaxAmount,
                LineTotal = line.LineTotal
            });
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _unitOfWork.PurchaseInvoices.Add(invoice);
            await _unitOfWork.SaveChangesAsync(ct); // assigns invoice.Id for the references below

            foreach (var line in lines)
            {
                await _inventoryService.PostTransactionAsync(
                    line.Product, InventoryTransactionType.Purchase, line.Quantity, line.UnitCost,
                    ReferenceType.PurchaseInvoice, invoice.Id, currentUserId,
                    $"Purchase — Invoice {invoiceNumber}", cancellationToken: ct);
            }

            if (dueAmount > 0)
            {
                await _supplierLedgerService.PostEntryAsync(
                    supplier.Id, SupplierLedgerEntryType.Purchase, ReferenceType.PurchaseInvoice, invoice.Id,
                    debit: 0, credit: dueAmount, currentUserId,
                    $"Purchase — Invoice {invoiceNumber}", ct);
            }
        }, cancellationToken);

        await _auditService.LogAsync(currentUserId, "PurchaseInvoicePosted", "PurchaseInvoice", invoice.Id, null, null,
            $"Posted purchase invoice {invoiceNumber} from {supplier.Name} — Total {grandTotal:N2}, Due {dueAmount:N2}.", cancellationToken);

        return invoice.Id;
    }

    public async Task<PurchaseInvoiceDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await _unitOfWork.PurchaseInvoices.GetByIdWithItemsAsync(id, cancellationToken);
        if (invoice is null) return null;

        return new PurchaseInvoiceDetailDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            SupplierName = invoice.Supplier?.Name ?? string.Empty,
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = invoice.PaidAmount,
            DueAmount = invoice.DueAmount,
            Status = invoice.Status,
            Notes = invoice.Notes,
            CreatedByUserName = invoice.CreatedByUser?.FullName ?? string.Empty,
            Items = invoice.Items.Select(i => new PurchaseInvoiceItemDetailDto
            {
                ProductName = i.ProductNameSnapshot,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                DiscountAmount = i.DiscountAmount,
                TaxAmount = i.TaxAmount,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }

    public async Task<(List<PurchaseInvoiceListItemDto> Items, int TotalCount)> SearchAsync(string? invoiceNumber, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (invoices, total) = await _unitOfWork.PurchaseInvoices.SearchAsync(invoiceNumber, null, fromDate, toDate, pageNumber, pageSize, cancellationToken);

        var items = invoices.Select(i => new PurchaseInvoiceListItemDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            SupplierName = i.Supplier?.Name ?? string.Empty,
            GrandTotal = i.GrandTotal,
            PaidAmount = i.PaidAmount,
            DueAmount = i.DueAmount,
            Status = i.Status,
            CreatedByUserName = i.CreatedByUser?.FullName ?? string.Empty
        }).ToList();

        return (items, total);
    }
}
