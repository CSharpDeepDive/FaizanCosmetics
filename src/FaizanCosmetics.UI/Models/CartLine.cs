using CommunityToolkit.Mvvm.ComponentModel;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.Models;

/// <summary>
/// One row in the New Invoice cart. Recomputes its own display totals live as Quantity/DiscountPercent
/// change, using the same ITaxCalculationService the server uses — so what the cashier sees before
/// posting matches what SalesInvoiceService will actually calculate, without duplicating the tax
/// formula itself. This is a client-side preview only: invoice-level discount distribution and the
/// final authoritative numbers are always (re)computed server-side in ISalesInvoiceService.PostInvoiceAsync.
/// </summary>
public partial class CartLine : ObservableObject
{
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly bool _taxEnabled;
    private readonly decimal _taxPercent;
    private readonly bool _taxInclusive;

    public CartLine(ProductDetailDto product, ITaxCalculationService taxCalculationService, bool taxEnabled, decimal taxPercent, bool taxInclusive)
    {
        ProductId = product.Id;
        ProductName = product.Name;
        Barcode = product.Barcode;
        AvailableStock = product.CurrentStock;
        UnitPrice = product.SellingPrice;
        Quantity = 1;

        _taxCalculationService = taxCalculationService;
        _taxEnabled = taxEnabled;
        _taxPercent = taxPercent;
        _taxInclusive = taxInclusive;

        Recalculate();
    }

    public int ProductId { get; }
    public string ProductName { get; }
    public string Barcode { get; }
    public decimal AvailableStock { get; }
    public decimal UnitPrice { get; }

    [ObservableProperty]
    private decimal quantity;

    [ObservableProperty]
    private decimal discountPercent;

    [ObservableProperty]
    private decimal lineSubtotal;

    [ObservableProperty]
    private decimal discountAmount;

    [ObservableProperty]
    private decimal taxAmount;

    [ObservableProperty]
    private decimal lineTotal;

    partial void OnQuantityChanged(decimal value) => Recalculate();
    partial void OnDiscountPercentChanged(decimal value) => Recalculate();

    private void Recalculate()
    {
        if (_taxCalculationService == null) return;
        var subtotal = Math.Round(Quantity * UnitPrice, 2);
        var discount = Math.Round(subtotal * DiscountPercent / 100m, 2);
        var taxable = subtotal - discount;
        var tax = _taxCalculationService.Calculate(taxable, _taxEnabled, _taxPercent, _taxInclusive);

        LineSubtotal = subtotal;
        DiscountAmount = discount;
        TaxAmount = tax.TaxAmount;
        LineTotal = tax.TotalAmount;
    }
}
