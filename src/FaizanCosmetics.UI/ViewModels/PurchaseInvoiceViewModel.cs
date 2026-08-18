using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.UI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FaizanCosmetics.UI.ViewModels;

/// <summary>
/// Purchase entry screen: pick a supplier, add product lines (via the product picker — no
/// barcode-scan urgency here, unlike Sales), set each line's negotiated cost, post. Mirrors
/// SalesInvoiceViewModel's structure without the barcode workflow, credit-limit check, or
/// invoice-level discount (purchases here support item-level discount only — see
/// IPurchaseInvoiceService's doc comment on the deliberately simpler scope).
/// </summary>
public partial class PurchaseInvoiceViewModel : ViewModelBase
{
    private readonly IPurchaseInvoiceService _purchaseInvoiceService;
    private readonly ISupplierService _supplierService;
    private readonly IProductService _productService;
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private bool _taxEnabled;
    private decimal _taxPercent;
    private bool _taxInclusive;

    public PurchaseInvoiceViewModel(
        IPurchaseInvoiceService purchaseInvoiceService,
        ISupplierService supplierService,
        IProductService productService,
        IAppSettingRepository appSettingRepository,
        ITaxCalculationService taxCalculationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
    {
        _purchaseInvoiceService = purchaseInvoiceService;
        _supplierService = supplierService;
        _productService = productService;
        _appSettingRepository = appSettingRepository;
        _taxCalculationService = taxCalculationService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        CartLines.CollectionChanged += (_, _) => RecalculateTotals();
        _ = InitializeAsync();
    }

    public ObservableCollection<PurchaseCartLine> CartLines { get; } = new();
    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = new[] { PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.BankTransfer };

    [ObservableProperty] private PurchaseCartLine? selectedLine;
    [ObservableProperty] private int? selectedSupplierId;
    [ObservableProperty] private string supplierDisplayName = "(no supplier selected)";
    [ObservableProperty] private decimal supplierOutstandingBalance;

    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal discountAmount;
    [ObservableProperty] private decimal taxAmount;
    [ObservableProperty] private decimal grandTotal;

    [ObservableProperty] private PaymentMethod selectedPaymentMethod = PaymentMethod.Cash;
    [ObservableProperty] private decimal paidAmount;
    [ObservableProperty] private decimal dueAmount;
    [ObservableProperty] private string? supplierInvoiceReference;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private string currencySymbol = "Rs.";

    public int? LastPostedInvoiceId { get; private set; }
    public event Action<int>? InvoicePosted;

    private async Task InitializeAsync()
    {
        var settings = await _appSettingRepository.GetAsync();
        _taxEnabled = settings.TaxEnabled;
        _taxPercent = settings.DefaultTaxPercent;
        _taxInclusive = settings.TaxInclusivePricing;
        CurrencySymbol = settings.CurrencySymbol;
    }

    [RelayCommand]
    private void SelectSupplier()
    {
        var dialog = _serviceProvider.GetRequiredService<Views.SupplierPickerWindow>();
        if (dialog.ShowDialog() == true && dialog.SelectedSupplierId.HasValue)
        {
            _ = ApplySelectedSupplierAsync(dialog.SelectedSupplierId.Value);
        }
    }

    private async Task ApplySelectedSupplierAsync(int supplierId)
    {
        var supplier = await _supplierService.GetByIdAsync(supplierId);
        if (supplier is null) return;

        SelectedSupplierId = supplier.Id;
        SupplierDisplayName = supplier.Name;
        SupplierOutstandingBalance = supplier.Balance;
    }

    [RelayCommand]
    private void AddProduct()
    {
        var dialog = _serviceProvider.GetRequiredService<Views.ProductPickerWindow>();
        if (dialog.ShowDialog() == true && dialog.SelectedProductId.HasValue)
        {
            _ = AddProductByIdAsync(dialog.SelectedProductId.Value);
        }
    }

    private async Task AddProductByIdAsync(int productId)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product is null) return;

        var existing = CartLines.FirstOrDefault(l => l.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += 1;
        }
        else
        {
            var line = new PurchaseCartLine(product, _taxCalculationService, _taxEnabled, _taxPercent, _taxInclusive);
            line.PropertyChanged += (_, _) => RecalculateTotals();
            CartLines.Add(line);
        }
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveLine()
    {
        if (SelectedLine is null) return;
        CartLines.Remove(SelectedLine);
        RecalculateTotals();
    }

    [RelayCommand]
    private void NewPurchase()
    {
        CartLines.Clear();
        PaidAmount = 0;
        Notes = null;
        SupplierInvoiceReference = null;
        SelectedPaymentMethod = PaymentMethod.Cash;
        SelectedSupplierId = null;
        SupplierDisplayName = "(no supplier selected)";
        SupplierOutstandingBalance = 0;
        ErrorMessage = null;
        RecalculateTotals();
    }

    [RelayCommand]
    private async Task PostInvoiceAsync()
    {
        ErrorMessage = null;

        if (SelectedSupplierId is null)
        {
            ErrorMessage = "Select a supplier before posting.";
            return;
        }
        if (CartLines.Count == 0)
        {
            ErrorMessage = "Add at least one item before posting.";
            return;
        }

        IsBusy = true;
        try
        {
            var currentUserId = _currentUser.UserId ?? throw new ValidationAppException("No user is signed in.");

            var invoiceId = await _purchaseInvoiceService.PostInvoiceAsync(new PostPurchaseInvoiceDto
            {
                SupplierId = SelectedSupplierId.Value,
                PaidAmount = PaidAmount,
                PaymentMethod = SelectedPaymentMethod,
                SupplierInvoiceReference = SupplierInvoiceReference,
                Notes = Notes,
                Items = CartLines.Select(l => new PurchaseInvoiceItemInputDto
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost,
                    DiscountPercent = l.DiscountPercent
                }).ToList()
            }, currentUserId);

            LastPostedInvoiceId = invoiceId;
            InvoicePosted?.Invoke(invoiceId);
            NewPurchase();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to post the purchase invoice right now. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RecalculateTotals()
    {
        SubTotal = CartLines.Sum(l => l.LineSubtotal);
        DiscountAmount = CartLines.Sum(l => l.DiscountAmount);
        TaxAmount = CartLines.Sum(l => l.TaxAmount);
        GrandTotal = Math.Max(0, SubTotal - DiscountAmount + TaxAmount);
        DueAmount = Math.Max(0, GrandTotal - PaidAmount);
    }

    partial void OnPaidAmountChanged(decimal value) => RecalculateTotals();
}
