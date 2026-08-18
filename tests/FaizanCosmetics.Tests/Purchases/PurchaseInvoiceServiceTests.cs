using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Infrastructure.Services;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FaizanCosmetics.Tests.Purchases;

public class PurchaseInvoiceServiceTests
{
    private class Fixture
    {
        public FaizanCosmetics.Infrastructure.Data.ApplicationDbContext Context { get; init; } = null!;
        public PurchaseInvoiceService Service { get; init; } = null!;
        public SupplierLedgerService LedgerService { get; init; } = null!;
        public int ProductId { get; set; }
        public int SupplierId { get; set; }
        public int UserId { get; set; }
    }

    private static async Task<Fixture> CreateFixtureAsync(decimal productStock = 10, decimal purchasePrice = 300)
    {
        var (context, unitOfWork) = TestUnitOfWorkFactory.Create();

        var category = new Category { Name = "Skin Care", IsActive = true };
        context.Categories.Add(category);

        var product = new Product
        {
            Name = "Face Cream", Barcode = "5556667778889", SKU = "FC-2", CategoryId = 0,
            PurchasePrice = purchasePrice, SellingPrice = purchasePrice + 200, WholesalePrice = purchasePrice + 150,
            CurrentStock = productStock, IsActive = true
        };
        product.Category = category;
        context.Products.Add(product);

        var supplier = new Supplier { Name = "Beauty Supplies Co", IsActive = true };
        context.Suppliers.Add(supplier);

        var user = new User { Username = "manager1", PasswordHash = "x", FullName = "Manager One", Role = UserRole.Manager, IsActive = true };
        context.Users.Add(user);

        await context.SaveChangesAsync();

        var settings = await context.AppSettings.FirstAsync();
        settings.TaxEnabled = true;
        settings.DefaultTaxPercent = 10;
        settings.TaxInclusivePricing = false;
        settings.PurchaseInvoicePrefix = "PINV";
        await context.SaveChangesAsync();

        var inventoryService = new InventoryService(context, unitOfWork.AppSettings);
        var ledgerService = new SupplierLedgerService(unitOfWork);
        var taxService = new TaxCalculationService(unitOfWork);
        var auditService = new TestAuditService();

        var service = new PurchaseInvoiceService(unitOfWork, inventoryService, ledgerService, taxService, auditService);

        return new Fixture
        {
            Context = context,
            Service = service,
            LedgerService = ledgerService,
            ProductId = product.Id,
            SupplierId = supplier.Id,
            UserId = user.Id
        };
    }

    [Fact]
    public async Task PostInvoiceAsync_CashPurchase_CalculatesTotalsAndIncreasesStock()
    {
        var f = await CreateFixtureAsync(productStock: 10, purchasePrice: 300);

        var invoiceId = await f.Service.PostInvoiceAsync(new PostPurchaseInvoiceDto
        {
            SupplierId = f.SupplierId,
            Items = { new PurchaseInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 5, UnitCost = 300 } },
            PaidAmount = 1650, // 5*300=1500 + 10% tax = 1650
            PaymentMethod = PaymentMethod.Cash
        }, f.UserId);

        var invoice = await f.Service.GetByIdAsync(invoiceId);
        invoice!.SubTotal.Should().Be(1500m);
        invoice.TaxAmount.Should().Be(150m);
        invoice.GrandTotal.Should().Be(1650m);
        invoice.DueAmount.Should().Be(0m);

        var product = await f.Context.Products.FindAsync(f.ProductId);
        product!.CurrentStock.Should().Be(15m);
    }

    [Fact]
    public async Task PostInvoiceAsync_WithItemDiscount_ReducesTaxableBase()
    {
        var f = await CreateFixtureAsync(purchasePrice: 1000);

        var invoiceId = await f.Service.PostInvoiceAsync(new PostPurchaseInvoiceDto
        {
            SupplierId = f.SupplierId,
            Items = { new PurchaseInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1, UnitCost = 1000, DiscountPercent = 10 } },
            PaidAmount = 990,
            PaymentMethod = PaymentMethod.Cash
        }, f.UserId);

        var invoice = await f.Service.GetByIdAsync(invoiceId);
        invoice!.DiscountAmount.Should().Be(100m);
        invoice.TaxAmount.Should().Be(90m);
        invoice.GrandTotal.Should().Be(990m);
    }

    [Fact]
    public async Task PostInvoiceAsync_PaidAmountExceedingGrandTotal_ThrowsPaymentExceedsDueException()
    {
        var f = await CreateFixtureAsync();

        var act = async () => await f.Service.PostInvoiceAsync(new PostPurchaseInvoiceDto
        {
            SupplierId = f.SupplierId,
            Items = { new PurchaseInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1, UnitCost = 300 } },
            PaidAmount = 999999,
            PaymentMethod = PaymentMethod.Cash
        }, f.UserId);

        await act.Should().ThrowAsync<PaymentExceedsDueException>();
    }

    [Fact]
    public async Task PostInvoiceAsync_PartialPayment_PostsSupplierLedgerCreditForDueAmount()
    {
        var f = await CreateFixtureAsync(purchasePrice: 500);

        var invoiceId = await f.Service.PostInvoiceAsync(new PostPurchaseInvoiceDto
        {
            SupplierId = f.SupplierId,
            Items = { new PurchaseInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 2, UnitCost = 500 } }, // 1000 + 10% = 1100
            PaidAmount = 600,
            PaymentMethod = PaymentMethod.Cash
        }, f.UserId);

        var invoice = await f.Service.GetByIdAsync(invoiceId);
        invoice!.DueAmount.Should().Be(500m);

        var balance = await f.LedgerService.GetBalanceAsync(f.SupplierId);
        balance.Should().Be(500m);
    }

    [Fact]
    public async Task PostInvoiceAsync_FullyPaid_DoesNotPostSupplierLedgerEntry()
    {
        var f = await CreateFixtureAsync(purchasePrice: 500);

        await f.Service.PostInvoiceAsync(new PostPurchaseInvoiceDto
        {
            SupplierId = f.SupplierId,
            Items = { new PurchaseInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1, UnitCost = 500 } }, // 550 total
            PaidAmount = 550,
            PaymentMethod = PaymentMethod.Cash
        }, f.UserId);

        var balance = await f.LedgerService.GetBalanceAsync(f.SupplierId);
        balance.Should().Be(0m, "a fully-paid purchase leaves nothing owed to the supplier");
    }

    [Fact]
    public async Task PostInvoiceAsync_DoesNotChangeProductStandingPurchasePrice()
    {
        var f = await CreateFixtureAsync(purchasePrice: 300);

        await f.Service.PostInvoiceAsync(new PostPurchaseInvoiceDto
        {
            SupplierId = f.SupplierId,
            Items = { new PurchaseInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1, UnitCost = 999 } }, // negotiated cost differs from standing price
            PaidAmount = 0,
            PaymentMethod = PaymentMethod.Cash
        }, f.UserId);

        var product = await f.Context.Products.FindAsync(f.ProductId);
        product!.PurchasePrice.Should().Be(300m, "receiving a purchase must not silently change the product's standing cost — that's IProductService.UpdateAsync's job, with price history logging");
    }
}
