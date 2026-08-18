using CommunityToolkit.Mvvm.ComponentModel;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.Models;

/// <summary>
/// One row in the Purchase entry cart. Mirrors CartLine's live-preview design, but UnitCost is
/// editable here (the negotiated cost for this receipt, defaulting to but independent from the
/// product's current PurchasePrice) rather than fixed to a selling price.
/// </summary>
public partial class PurchaseCartLine : ObservableObject
{
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly bool _taxEnabled;
    private readonly decimal _taxPercent;
    private readonly bool _taxInclusive;

    public PurchaseCartLine(ProductDetailDto product, ITaxCalculationService taxCalculationService, bool taxEnabled, decimal taxPercent, bool taxInclusive)
    {
        ProductId = product.Id;
        ProductName = product.Name;

        _taxCalculationService = taxCalculationService;
        _taxEnabled = taxEnabled;
        _taxPercent = taxPercent;
        _taxInclusive = taxInclusive;

        Quantity = 1;
        UnitCost = product.PurchasePrice;

        Recalculate();
    }

    public int ProductId { get; }
    public string ProductName { get; }

    [ObservableProperty]
    private decimal quantity;

    [ObservableProperty]
    private decimal unitCost;

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
    partial void OnUnitCostChanged(decimal value) => Recalculate();
    partial void OnDiscountPercentChanged(decimal value) => Recalculate();

    private void Recalculate()
    {
        var subtotal = Math.Round(Quantity * UnitCost, 2);
        var discount = Math.Round(subtotal * DiscountPercent / 100m, 2);
        var taxable = subtotal - discount;
        var tax = _taxCalculationService.Calculate(taxable, _taxEnabled, _taxPercent, _taxInclusive);

        LineSubtotal = subtotal;
        DiscountAmount = discount;
        TaxAmount = tax.TaxAmount;
        LineTotal = tax.TotalAmount;
    }
}
