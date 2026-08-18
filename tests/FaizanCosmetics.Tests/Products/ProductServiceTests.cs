using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Infrastructure.Services;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Products;

public class ProductServiceTests
{
    private static (ProductService Service, FaizanCosmetics.Infrastructure.Data.ApplicationDbContext Context) CreateService()
    {
        var (context, unitOfWork) = TestUnitOfWorkFactory.Create();
        var inventoryService = new InventoryService(context, unitOfWork.AppSettings);
        var auditService = new TestAuditService();
        var currentUser = new TestCurrentUserService();
        var service = new ProductService(unitOfWork, inventoryService, auditService, currentUser);
        return (service, context);
    }

    private static async Task<int> SeedCategoryAsync(FaizanCosmetics.Infrastructure.Data.ApplicationDbContext context)
    {
        var category = new Category { Name = "Skin Care", IsActive = true };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category.Id;
    }

    [Fact]
    public async Task CreateAsync_WithOpeningStock_PersistsProductAndPostsInventoryTransaction()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        var productId = await service.CreateAsync(new CreateProductDto
        {
            Name = "Rose Face Wash",
            Barcode = "8901234567890",
            SKU = "SKU-001",
            CategoryId = categoryId,
            PurchasePrice = 200m,
            SellingPrice = 350m,
            WholesalePrice = 280m,
            MinimumStockLevel = 10,
            ReorderLevel = 20,
            OpeningStock = 50
        }, currentUserId: 1);

        var product = await service.GetByIdAsync(productId);
        product.Should().NotBeNull();
        product!.CurrentStock.Should().Be(50m);

        var transactions = context.InventoryTransactions.Where(t => t.ProductId == productId).ToList();
        transactions.Should().ContainSingle();
        transactions[0].TransactionType.Should().Be(InventoryTransactionType.OpeningStock);
        transactions[0].NewStock.Should().Be(50m);
        transactions[0].PreviousStock.Should().Be(0m);
    }

    [Fact]
    public async Task CreateAsync_WithZeroOpeningStock_PersistsProductWithoutInventoryTransaction()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        var productId = await service.CreateAsync(new CreateProductDto
        {
            Name = "Lavender Shampoo",
            Barcode = "8901234500001",
            SKU = "SKU-002",
            CategoryId = categoryId,
            PurchasePrice = 150m,
            SellingPrice = 250m,
            WholesalePrice = 200m,
            OpeningStock = 0
        }, currentUserId: 1);

        var product = await service.GetByIdAsync(productId);
        product!.CurrentStock.Should().Be(0m);
        context.InventoryTransactions.Any(t => t.ProductId == productId).Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateBarcode_ThrowsDuplicateBarcodeException()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        var dto = new CreateProductDto
        {
            Name = "Product A",
            Barcode = "1111111111111",
            SKU = "SKU-A",
            CategoryId = categoryId,
            PurchasePrice = 10,
            SellingPrice = 20,
            WholesalePrice = 15
        };
        await service.CreateAsync(dto, currentUserId: 1);

        var duplicate = new CreateProductDto
        {
            Name = "Product B",
            Barcode = "1111111111111", // same barcode
            SKU = "SKU-B",
            CategoryId = categoryId,
            PurchasePrice = 10,
            SellingPrice = 20,
            WholesalePrice = 15
        };

        var act = async () => await service.CreateAsync(duplicate, currentUserId: 1);
        await act.Should().ThrowAsync<DuplicateBarcodeException>();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSku_ThrowsDuplicateSkuException()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        await service.CreateAsync(new CreateProductDto
        {
            Name = "Product A", Barcode = "2222222222220", SKU = "SKU-DUP",
            CategoryId = categoryId, PurchasePrice = 10, SellingPrice = 20, WholesalePrice = 15
        }, currentUserId: 1);

        var act = async () => await service.CreateAsync(new CreateProductDto
        {
            Name = "Product B", Barcode = "2222222222221", SKU = "SKU-DUP",
            CategoryId = categoryId, PurchasePrice = 10, SellingPrice = 20, WholesalePrice = 15
        }, currentUserId: 1);

        await act.Should().ThrowAsync<DuplicateSkuException>();
    }

    [Fact]
    public async Task SearchAsync_FindsProductByPartialBarcodeSkuOrName()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        await service.CreateAsync(new CreateProductDto
        {
            Name = "Almond Body Lotion", Barcode = "9990001112223", SKU = "ABL-100",
            CategoryId = categoryId, PurchasePrice = 300, SellingPrice = 500, WholesalePrice = 420
        }, currentUserId: 1);

        (await service.SearchAsync("Almond", null, true, 1, 20)).Items.Should().ContainSingle();
        (await service.SearchAsync("999000", null, true, 1, 20)).Items.Should().ContainSingle();
        (await service.SearchAsync("ABL-100", null, true, 1, 20)).Items.Should().ContainSingle();
        (await service.SearchAsync("NoSuchThing", null, true, 1, 20)).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WhenPriceChanges_RecordsPriceHistoryEntry()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        var productId = await service.CreateAsync(new CreateProductDto
        {
            Name = "Vitamin C Serum", Barcode = "7778889990001", SKU = "VCS-1",
            CategoryId = categoryId, PurchasePrice = 400, SellingPrice = 700, WholesalePrice = 550
        }, currentUserId: 1);

        await service.UpdateAsync(new UpdateProductDto
        {
            Id = productId,
            Name = "Vitamin C Serum",
            Barcode = "7778889990001",
            SKU = "VCS-1",
            CategoryId = categoryId,
            PurchasePrice = 420, // changed
            SellingPrice = 750,  // changed
            WholesalePrice = 550,
            PriceChangeReason = PriceChangeReason.SupplierPriceUpdate
        }, currentUserId: 1);

        var history = await service.GetPriceHistoryAsync(productId);
        history.Should().ContainSingle();
        history[0].OldPurchasePrice.Should().Be(400);
        history[0].NewPurchasePrice.Should().Be(420);
        history[0].OldSellingPrice.Should().Be(700);
        history[0].NewSellingPrice.Should().Be(750);
    }

    [Fact]
    public async Task UpdateAsync_WhenPricesUnchanged_DoesNotRecordPriceHistory()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        var productId = await service.CreateAsync(new CreateProductDto
        {
            Name = "Argan Hair Oil", Barcode = "6667778880002", SKU = "AHO-1",
            CategoryId = categoryId, PurchasePrice = 250, SellingPrice = 450, WholesalePrice = 380
        }, currentUserId: 1);

        await service.UpdateAsync(new UpdateProductDto
        {
            Id = productId,
            Name = "Argan Hair Oil (Updated Description)",
            Barcode = "6667778880002",
            SKU = "AHO-1",
            CategoryId = categoryId,
            PurchasePrice = 250,
            SellingPrice = 450,
            WholesalePrice = 380
        }, currentUserId: 1);

        var history = await service.GetPriceHistoryAsync(productId);
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLowStockAsync_ReturnsOnlyProductsAtOrBelowMinimumButAboveZero()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        await service.CreateAsync(new CreateProductDto
        {
            Name = "Low Stock Item", Barcode = "1010101010101", SKU = "LSI-1",
            CategoryId = categoryId, PurchasePrice = 10, SellingPrice = 20, WholesalePrice = 15,
            MinimumStockLevel = 10, OpeningStock = 5
        }, currentUserId: 1);

        await service.CreateAsync(new CreateProductDto
        {
            Name = "Healthy Stock Item", Barcode = "2020202020202", SKU = "HSI-1",
            CategoryId = categoryId, PurchasePrice = 10, SellingPrice = 20, WholesalePrice = 15,
            MinimumStockLevel = 10, OpeningStock = 100
        }, currentUserId: 1);

        await service.CreateAsync(new CreateProductDto
        {
            Name = "Out Of Stock Item", Barcode = "3030303030303", SKU = "OOS-1",
            CategoryId = categoryId, PurchasePrice = 10, SellingPrice = 20, WholesalePrice = 15,
            MinimumStockLevel = 10, OpeningStock = 0
        }, currentUserId: 1);

        var lowStock = await service.GetLowStockAsync();
        lowStock.Should().ContainSingle(p => p.Name == "Low Stock Item");

        var outOfStock = await service.GetOutOfStockAsync();
        outOfStock.Should().ContainSingle(p => p.Name == "Out Of Stock Item");
    }

    [Fact]
    public async Task DeactivateAsync_SetsProductInactive_ButDoesNotDeleteIt()
    {
        var (service, context) = CreateService();
        var categoryId = await SeedCategoryAsync(context);

        var productId = await service.CreateAsync(new CreateProductDto
        {
            Name = "Charcoal Soap", Barcode = "4040404040404", SKU = "CS-1",
            CategoryId = categoryId, PurchasePrice = 50, SellingPrice = 90, WholesalePrice = 75
        }, currentUserId: 1);

        await service.DeactivateAsync(productId);

        var product = await service.GetByIdAsync(productId);
        product.Should().NotBeNull("deactivation must never delete the record");
        product!.IsActive.Should().BeFalse();
    }
}
