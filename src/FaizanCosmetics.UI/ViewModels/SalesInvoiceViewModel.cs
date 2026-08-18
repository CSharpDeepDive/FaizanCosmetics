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
/// The cashier's main screen. Barcode → Product → Quantity → Payment → Post, optimized for
/// keyboard-only operation (see SalesInvoiceView.xaml's InputBindings for F2–F8/Esc). Client-side
/// totals are a live preview computed via CartLine; the authoritative numbers always come back
/// from ISalesInvoiceService.PostInvoiceAsync, which is free to (and does) compute them slightly
/// differently once invoice-level discount distribution is involved.
/// </summary>
public partial class SalesInvoiceViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly ISalesInvoiceService _salesInvoiceService;
    private readonly IClientService _clientService;
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private bool _taxEnabled;
    private decimal _taxPercent;
    private bool _taxInclusive;
    private string _currencySymbol = "Rs.";

    public SalesInvoiceViewModel(
        IProductService productService,
        ISalesInvoiceService salesInvoiceService,
        IClientService clientService,
        IAppSettingRepository appSettingRepository,
        ITaxCalculationService taxCalculationService,
        ICurrentUserService currentUser,
        IServiceProvider serviceProvider)
    {
        _productService = productService;
        _salesInvoiceService = salesInvoiceService;
        _clientService = clientService;
        _appSettingRepository = appSettingRepository;
        _taxCalculationService = taxCalculationService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        CartLines.CollectionChanged += (_, _) => RecalculateTotals();
        _ = InitializeAsync();
    }

    public ObservableCollection<CartLine> CartLines { get; } = new();

    [ObservableProperty] private string barcodeInput = string.Empty;
    [ObservableProperty] private CartLine? selectedLine;

    [ObservableProperty] private int? selectedClientId;
    [ObservableProperty] private string clientDisplayName = "Walk-in Customer";
    [ObservableProperty] private decimal clientOutstandingBalance;
    [ObservableProperty] private decimal clientCreditLimit;
    [ObservableProperty] private bool clientIsWalkIn = true;

    [ObservableProperty] private decimal invoiceDiscountPercent;
    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal discountAmount;
    [ObservableProperty] private decimal taxAmount;
    [ObservableProperty] private decimal grandTotal;

    [ObservableProperty] private PaymentMethod selectedPaymentMethod = PaymentMethod.Cash;
    [ObservableProperty] private decimal paidAmount;
    [ObservableProperty] private decimal dueAmount;
    [ObservableProperty] private string? notes;

    [ObservableProperty] private string currencySymbol = "Rs.";

    private async Task InitializeAsync()
    {
        var settings = await _appSettingRepository.GetAsync();
        _taxEnabled = settings.TaxEnabled;
        _taxPercent = settings.DefaultTaxPercent;
        _taxInclusive = settings.TaxInclusivePricing;
        CurrencySymbol = settings.CurrencySymbol;
        _currencySymbol = settings.CurrencySymbol;

        await SetWalkInClientAsync();
    }

    private async Task SetWalkInClientAsync()
    {
        SelectedClientId = null;
        ClientDisplayName = "Walk-in Customer";
        ClientIsWalkIn = true;
        ClientOutstandingBalance = 0;
        ClientCreditLimit = 0;
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        var barcode = BarcodeInput.Trim();
        BarcodeInput = string.Empty;
        if (string.IsNullOrEmpty(barcode)) return;

        ErrorMessage = null;
        var product = await _productService.GetByBarcodeAsync(barcode);
        if (product is null)
        {
            ErrorMessage = $"Product Not Found: no active product matches barcode '{barcode}'.";
            return;
        }

        AddOrIncrementLine(product);
    }

    private void AddOrIncrementLine(ProductDetailDto product)
    {
        var existing = CartLines.FirstOrDefault(l => l.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += 1;
        }
        else
        {
            var line = new CartLine(product, _taxCalculationService, _taxEnabled, _taxPercent, _taxInclusive);
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
    private void OpenProductPicker()
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
        if (product is not null) AddOrIncrementLine(product);
    }

    [RelayCommand]
    private void Print() => ErrorMessage = "Receipt printing arrives in Phase 9 — Printing, Excel/PDF Export, Backup/Restore.";

    /// <summary>Esc: clears the current barcode entry and any error message, without discarding the cart — a full clear is F5 (New Invoice), a deliberately separate and heavier action.</summary>
    [RelayCommand]
    private void CancelOperation()
    {
        BarcodeInput = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void SelectClient()
    {
        var dialog = _serviceProvider.GetRequiredService<Views.ClientPickerWindow>();
        if (dialog.ShowDialog() == true && dialog.SelectedClientId.HasValue)
        {
            _ = ApplySelectedClientAsync(dialog.SelectedClientId.Value);
        }
    }

    private async Task ApplySelectedClientAsync(int clientId)
    {
        var client = await _clientService.GetByIdAsync(clientId);
        if (client is null) return;

        SelectedClientId = client.Id;
        ClientDisplayName = client.Name;
        ClientIsWalkIn = client.IsWalkInCustomer;
        ClientOutstandingBalance = client.Balance;
        ClientCreditLimit = client.CreditLimit;
    }

    [RelayCommand]
    private void ClearClient() => _ = SetWalkInClientAsync();

    [RelayCommand]
    private void NewInvoice()
    {
        CartLines.Clear();
        InvoiceDiscountPercent = 0;
        PaidAmount = 0;
        Notes = null;
        SelectedPaymentMethod = PaymentMethod.Cash;
        ErrorMessage = null;
        _ = SetWalkInClientAsync();
        RecalculateTotals();
    }

    [RelayCommand]
    private async Task PostInvoiceAsync()
    {
        ErrorMessage = null;

        if (CartLines.Count == 0)
        {
            ErrorMessage = "Add at least one item before posting.";
            return;
        }

        IsBusy = true;
        try
        {
            var currentUserId = _currentUser.UserId ?? throw new ValidationAppException("No user is signed in.");

            var invoiceId = await _salesInvoiceService.PostInvoiceAsync(new PostSalesInvoiceDto
            {
                ClientId = SelectedClientId,
                InvoiceDiscountPercent = InvoiceDiscountPercent,
                PaidAmount = PaidAmount,
                PaymentMethod = SelectedPaymentMethod,
                Notes = Notes,
                Items = CartLines.Select(l => new SalesInvoiceItemInputDto
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    DiscountPercent = l.DiscountPercent
                }).ToList()
            }, currentUserId);

            LastPostedInvoiceId = invoiceId;
            InvoicePosted?.Invoke(invoiceId);
            NewInvoice();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to post the invoice right now. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public int? LastPostedInvoiceId { get; private set; }
    public event Action<int>? InvoicePosted;

    private void RecalculateTotals()
    {
        SubTotal = CartLines.Sum(l => l.LineSubtotal);

        var netAfterItemDiscount = CartLines.Sum(l => l.LineSubtotal - l.DiscountAmount);
        var invoiceDiscount = netAfterItemDiscount > 0 ? Math.Round(netAfterItemDiscount * InvoiceDiscountPercent / 100m, 2) : 0;

        DiscountAmount = CartLines.Sum(l => l.DiscountAmount) + invoiceDiscount;
        TaxAmount = CartLines.Sum(l => l.TaxAmount); // preview only — server recalculates tax on the post-invoice-discount base
        GrandTotal = Math.Max(0, SubTotal - DiscountAmount + TaxAmount);
        DueAmount = Math.Max(0, GrandTotal - PaidAmount);
    }

    partial void OnInvoiceDiscountPercentChanged(decimal value) => RecalculateTotals();
    partial void OnPaidAmountChanged(decimal value) => RecalculateTotals();
}
