using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Categories;

public class CategoryServiceTests
{
    private static (CategoryService Service, FaizanCosmetics.Infrastructure.Data.ApplicationDbContext Context) CreateService()
    {
        var (context, unitOfWork) = TestUnitOfWorkFactory.Create();
        var service = new CategoryService(unitOfWork, new TestAuditService(), new TestCurrentUserService());
        return (service, context);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsValidationAppException()
    {
        var (service, _) = CreateService();
        await service.CreateAsync("Perfumes", null);

        var act = async () => await service.CreateAsync("Perfumes", "duplicate");
        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeactivateAsync_WithProductsStillAssigned_ThrowsValidationAppException()
    {
        var (service, context) = CreateService();
        var categoryId = await service.CreateAsync("Hair Care", null);

        context.Products.Add(new Product
        {
            Name = "Shampoo", Barcode = "1231231231230", SKU = "SH-1",
            CategoryId = categoryId, PurchasePrice = 100, SellingPrice = 180, WholesalePrice = 150,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var act = async () => await service.DeactivateAsync(categoryId);
        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeactivateAsync_WithNoProducts_Succeeds()
    {
        var (service, _) = CreateService();
        var categoryId = await service.CreateAsync("Accessories", null);

        await service.DeactivateAsync(categoryId);

        var categories = await service.GetAllAsync(activeOnly: false);
        categories.Should().ContainSingle(c => c.Id == categoryId && !c.IsActive);
    }
}
