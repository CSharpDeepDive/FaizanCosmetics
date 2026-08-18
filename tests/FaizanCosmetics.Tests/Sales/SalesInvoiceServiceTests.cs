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

namespace FaizanCosmetics.Tests.Sales;

public class SalesInvoiceServiceTests
{
    private class Fixture
    {
        public FaizanCosmetics.Infrastructure.Data.ApplicationDbContext Context { get; init; } = null!;
        public SalesInvoiceService Service { get; init; } = null!;
        public ClientLedgerService LedgerService { get; init; } = null!;
        public TestCurrentUserService CurrentUser { get; init; } = null!;
        public int ProductId { get; set; }
        public int ClientId { get; set; }
        public int WalkInClientId { get; set; }
    }

    private static async Task<Fixture> CreateFixtureAsync(decimal productStock = 100, decimal sellingPrice = 500, decimal purchasePrice = 300, decimal clientCreditLimit = 10000)
    {
        var (context, unitOfWork) = TestUnitOfWorkFactory.Create();

        var category = new Category { Name = "Skin Care", IsActive = true };
        context.Categories.Add(category);

        var product = new Product
        {
            Name = "Face Cream", Barcode = "1112223334445", SKU = "FC-1", CategoryId = 0,
            PurchasePrice = purchasePrice, SellingPrice = sellingPrice, WholesalePrice = sellingPrice - 50,
            CurrentStock = productStock, MinimumStockLevel = 5, IsActive = true
        };
        product.Category = category;
        context.Products.Add(product);

        var client = new Client
        {
            ClientCode = "CL-000001", Name = "Regular Client", IsActive = true,
            ClientType = ClientType.Retail, CreditLimit = clientCreditLimit
        };
        context.Clients.Add(client);

        var walkIn = new Client
        {
            ClientCode = "CL-000000", Name = "Walk-in Customer", IsActive = true,
            ClientType = ClientType.Retail, IsWalkInCustomer = true
        };
        context.Clients.Add(walkIn);

        var user = new User { Username = "cashier1", PasswordHash = "x", FullName = "Cashier One", Role = UserRole.Cashier, IsActive = true };
        context.Users.Add(user);

        await context.SaveChangesAsync();

        var settings = await context.AppSettings.FirstAsync();
        settings.TaxEnabled = true;
        settings.DefaultTaxPercent = 10;
        settings.TaxInclusivePricing = false;
        settings.InvoicePrefix = "INV";
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();
        currentUser.SetCurrentUser(user.Id, user.Username, user.FullName, UserRole.Cashier, 0, canOverrideCreditLimit: false);

        var inventoryService = new InventoryService(context, unitOfWork.AppSettings);
        var ledgerService = new ClientLedgerService(unitOfWork);
        var taxService = new TaxCalculationService(unitOfWork);
        var auditService = new TestAuditService();

        var service = new SalesInvoiceService(unitOfWork, inventoryService, ledgerService, taxService, auditService, currentUser);

        return new Fixture
        {
            Context = context,
            Service = service,
            LedgerService = ledgerService,
            CurrentUser = currentUser,
            ProductId = product.Id,
            ClientId = client.Id,
            WalkInClientId = walkIn.Id
        };
    }

    [Fact]
    public async Task PostInvoiceAsync_CashSale_CalculatesTotalsAndDeductsStock()
    {
        var f = await CreateFixtureAsync(productStock: 50, sellingPrice: 500);

        var invoiceId = await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            ClientId = null, // walk-in
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 2, DiscountPercent = 0 } },
            PaidAmount = 1100, // 2*500=1000 + 10% tax=100
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        var invoice = await f.Service.GetByIdAsync(invoiceId);
        invoice.Should().NotBeNull();
        invoice!.SubTotal.Should().Be(1000m);
        invoice.TaxAmount.Should().Be(100m);
        invoice.GrandTotal.Should().Be(1100m);
        invoice.DueAmount.Should().Be(0m);
        invoice.PaymentStatus.Should().Be(PaymentStatus.Paid);

        var product = await f.Context.Products.FindAsync(f.ProductId);
        product!.CurrentStock.Should().Be(48m);
    }

    [Fact]
    public async Task PostInvoiceAsync_WithItemDiscount_ReducesTaxableBaseAndTotal()
    {
        var f = await CreateFixtureAsync(sellingPrice: 1000);

        var invoiceId = await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1, DiscountPercent = 10 } },
            PaidAmount = 990, // 1000 - 100 discount = 900, +10% tax = 990
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        var invoice = await f.Service.GetByIdAsync(invoiceId);
        invoice!.DiscountAmount.Should().Be(100m);
        invoice.TaxAmount.Should().Be(90m);
        invoice.GrandTotal.Should().Be(990m);
    }

    [Fact]
    public async Task PostInvoiceAsync_PaidAmountExceedingGrandTotal_ThrowsPaymentExceedsDueException()
    {
        var f = await CreateFixtureAsync(sellingPrice: 500);

        var act = async () => await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1 } },
            PaidAmount = 999999,
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        await act.Should().ThrowAsync<PaymentExceedsDueException>();
    }

    [Fact]
    public async Task PostInvoiceAsync_WalkInCustomerWithDueBalance_ThrowsValidationAppException()
    {
        var f = await CreateFixtureAsync(sellingPrice: 500);

        var act = async () => await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            ClientId = f.WalkInClientId,
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1 } },
            PaidAmount = 0, // full due balance
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task PostInvoiceAsync_InsufficientStock_ThrowsInsufficientStockException_AndDoesNotCreateInvoice()
    {
        var f = await CreateFixtureAsync(productStock: 1, sellingPrice: 500);

        var act = async () => await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 5 } },
            PaidAmount = 0,
            ClientId = f.ClientId,
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        await act.Should().ThrowAsync<InsufficientStockException>();

        var (invoices, total) = await f.Service.SearchAsync(null, null, null, 1, 20);
        total.Should().Be(0, "a failed post must not leave a partial invoice behind");
    }

    [Fact]
    public async Task PostInvoiceAsync_CreditSale_PostsClientLedgerDebitForDueAmount()
    {
        var f = await CreateFixtureAsync(sellingPrice: 500, clientCreditLimit: 10000);

        var invoiceId = await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            ClientId = f.ClientId,
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 2 } }, // 1000 + 10% tax = 1100
            PaidAmount = 600,
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        var invoice = await f.Service.GetByIdAsync(invoiceId);
        invoice!.DueAmount.Should().Be(500m);
        invoice.PaymentStatus.Should().Be(PaymentStatus.Partial);

        var balance = await f.LedgerService.GetBalanceAsync(f.ClientId);
        balance.Should().Be(500m);
    }

    [Fact]
    public async Task PostInvoiceAsync_CreditSaleExceedingCreditLimit_ThrowsWhenCannotOverride()
    {
        var f = await CreateFixtureAsync(sellingPrice: 5000, clientCreditLimit: 1000);
        // Cashier's TestCurrentUserService was set with canOverrideCreditLimit: false

        var act = async () => await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            ClientId = f.ClientId,
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1 } }, // 5000 + 10% = 5500 due, way over 1000 limit
            PaidAmount = 0,
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        await act.Should().ThrowAsync<CreditLimitExceededException>();
    }

    [Fact]
    public async Task PostInvoiceAsync_CreditSaleExceedingCreditLimit_SucceedsWhenCanOverride()
    {
        var f = await CreateFixtureAsync(sellingPrice: 5000, clientCreditLimit: 1000);
        f.CurrentUser.SetCurrentUser(f.CurrentUser.UserId!.Value, "manager1", "Manager One", UserRole.Manager, 0, canOverrideCreditLimit: true);

        var invoiceId = await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            ClientId = f.ClientId,
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1 } },
            PaidAmount = 0,
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        invoiceId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CancelAsync_ByCashier_ThrowsValidationAppException()
    {
        var f = await CreateFixtureAsync(sellingPrice: 500);
        var invoiceId = await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1 } },
            PaidAmount = 550,
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        // CurrentUser is still Cashier from fixture setup
        var act = async () => await f.Service.CancelAsync(invoiceId, "Customer changed mind", f.CurrentUser.UserId!.Value);
        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CancelAsync_ByManager_ReversesStockAndLedger()
    {
        var f = await CreateFixtureAsync(productStock: 20, sellingPrice: 500, clientCreditLimit: 10000);

        var invoiceId = await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            ClientId = f.ClientId,
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 3 } }, // 1500 + 10% = 1650
            PaidAmount = 500,
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        var stockAfterSale = (await f.Context.Products.FindAsync(f.ProductId))!.CurrentStock;
        stockAfterSale.Should().Be(17m);
        var balanceAfterSale = await f.LedgerService.GetBalanceAsync(f.ClientId);
        balanceAfterSale.Should().Be(1150m); // 1650 - 500 paid

        f.CurrentUser.SetCurrentUser(f.CurrentUser.UserId!.Value, "manager1", "Manager One", UserRole.Manager, 0, true);
        await f.Service.CancelAsync(invoiceId, "Wrong items scanned", f.CurrentUser.UserId!.Value);

        var stockAfterCancel = (await f.Context.Products.FindAsync(f.ProductId))!.CurrentStock;
        stockAfterCancel.Should().Be(20m, "cancellation must fully restore the stock that was sold");

        var balanceAfterCancel = await f.LedgerService.GetBalanceAsync(f.ClientId);
        balanceAfterCancel.Should().Be(0m, "cancellation must reverse the Khata debit exactly");

        var invoice = await f.Service.GetByIdAsync(invoiceId);
        invoice!.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.CancellationReason.Should().Be("Wrong items scanned");
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelledInvoice_ThrowsValidationAppException()
    {
        var f = await CreateFixtureAsync(sellingPrice: 500);
        var invoiceId = await f.Service.PostInvoiceAsync(new PostSalesInvoiceDto
        {
            Items = { new SalesInvoiceItemInputDto { ProductId = f.ProductId, Quantity = 1 } },
            PaidAmount = 550,
            PaymentMethod = PaymentMethod.Cash
        }, f.CurrentUser.UserId!.Value);

        f.CurrentUser.SetCurrentUser(f.CurrentUser.UserId!.Value, "manager1", "Manager One", UserRole.Manager, 0, true);
        await f.Service.CancelAsync(invoiceId, "First cancel", f.CurrentUser.UserId!.Value);

        var act = async () => await f.Service.CancelAsync(invoiceId, "Second cancel attempt", f.CurrentUser.UserId!.Value);
        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
