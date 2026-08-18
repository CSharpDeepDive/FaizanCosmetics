using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Infrastructure.Repositories;
using FaizanCosmetics.Infrastructure.Services;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FaizanCosmetics.Tests.Inventory;

public class InventoryServiceTests
{
    private static async Task<(FaizanCosmetics.Infrastructure.Data.ApplicationDbContext Context, InventoryService Service, Product Product)> CreateWithProductAsync(decimal startingStock)
    {
        var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Test Category", IsActive = true };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var user = new User { Username = "tester", PasswordHash = "x", FullName = "Tester", Role = UserRole.Admin, IsActive = true };
        context.Users.Add(user);

        var product = new Product
        {
            Name = "Test Product", Barcode = "0000000000001", SKU = "TP-1",
            CategoryId = category.Id, PurchasePrice = 100, SellingPrice = 150, WholesalePrice = 120,
            CurrentStock = startingStock, IsActive = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var appSettingRepo = new AppSettingRepository(context);
        var service = new InventoryService(context, appSettingRepo);
        return (context, service, product);
    }

    [Fact]
    public async Task PostTransactionAsync_Sale_DeductsStockAndRecordsTransaction()
    {
        var (context, service, product) = await CreateWithProductAsync(startingStock: 20);

        var transaction = await service.PostTransactionAsync(
            product, InventoryTransactionType.Sale, quantity: 5, unitCost: 100,
            ReferenceType.SalesInvoice, referenceId: 1, userId: 1);
        await context.SaveChangesAsync();

        product.CurrentStock.Should().Be(15);
        transaction.PreviousStock.Should().Be(20);
        transaction.NewStock.Should().Be(15);
        transaction.TransactionType.Should().Be(InventoryTransactionType.Sale);
    }

    [Fact]
    public async Task PostTransactionAsync_SaleExceedingStock_ThrowsInsufficientStockException_WhenNegativeStockNotAllowed()
    {
        var (context, service, product) = await CreateWithProductAsync(startingStock: 3);

        var act = async () => await service.PostTransactionAsync(
            product, InventoryTransactionType.Sale, quantity: 10, unitCost: 100,
            ReferenceType.SalesInvoice, referenceId: 1, userId: 1);

        await act.Should().ThrowAsync<InsufficientStockException>();
        product.CurrentStock.Should().Be(3, "a rejected transaction must not mutate stock");
    }

    [Fact]
    public async Task PostTransactionAsync_SaleExceedingStock_SucceedsWhenNegativeStockAllowed()
    {
        var (context, service, product) = await CreateWithProductAsync(startingStock: 3);

        var settings = await context.AppSettings.FirstAsync();
        settings.AllowNegativeStock = true;
        await context.SaveChangesAsync();

        var transaction = await service.PostTransactionAsync(
            product, InventoryTransactionType.Sale, quantity: 10, unitCost: 100,
            ReferenceType.SalesInvoice, referenceId: 1, userId: 1);

        transaction.NewStock.Should().Be(-7);
        product.CurrentStock.Should().Be(-7);
    }

    [Fact]
    public async Task PostTransactionAsync_Purchase_IncreasesStock()
    {
        var (context, service, product) = await CreateWithProductAsync(startingStock: 10);

        var transaction = await service.PostTransactionAsync(
            product, InventoryTransactionType.Purchase, quantity: 25, unitCost: 95,
            ReferenceType.PurchaseInvoice, referenceId: 1, userId: 1);

        transaction.NewStock.Should().Be(35);
        product.CurrentStock.Should().Be(35);
    }

    [Fact]
    public async Task PostTransactionAsync_ZeroOrNegativeQuantity_ThrowsValidationAppException()
    {
        var (context, service, product) = await CreateWithProductAsync(startingStock: 10);

        var act = async () => await service.PostTransactionAsync(
            product, InventoryTransactionType.Purchase, quantity: 0, unitCost: 95,
            ReferenceType.PurchaseInvoice, referenceId: 1, userId: 1);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Theory]
    [InlineData(InventoryTransactionType.Purchase, true)]
    [InlineData(InventoryTransactionType.SaleReturn, true)]
    [InlineData(InventoryTransactionType.AdjustmentIncrease, true)]
    [InlineData(InventoryTransactionType.OpeningStock, true)]
    [InlineData(InventoryTransactionType.Sale, false)]
    [InlineData(InventoryTransactionType.PurchaseReturn, false)]
    [InlineData(InventoryTransactionType.AdjustmentDecrease, false)]
    [InlineData(InventoryTransactionType.Damage, false)]
    [InlineData(InventoryTransactionType.Theft, false)]
    [InlineData(InventoryTransactionType.Expiry, false)]
    public void IsIncreaseType_MatchesExpectedDirection(InventoryTransactionType type, bool expectedIncrease)
    {
        var context = TestDbContextFactory.Create();
        var service = new InventoryService(context, new AppSettingRepository(context));

        service.IsIncreaseType(type).Should().Be(expectedIncrease);
    }
}
