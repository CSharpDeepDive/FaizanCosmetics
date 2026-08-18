using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Tests.Common;
using FluentAssertions;
using Xunit;

namespace FaizanCosmetics.Tests.Sales;

public class TaxCalculationServiceTests
{
    [Fact]
    public void Calculate_TaxDisabled_ReturnsZeroTaxAndUnchangedTotal()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var service = new TaxCalculationService(unitOfWork);

        var result = service.Calculate(1000m, taxEnabled: false, taxPercent: 15, taxInclusive: false);

        result.TaxAmount.Should().Be(0m);
        result.TotalAmount.Should().Be(1000m);
    }

    [Fact]
    public void Calculate_ExclusiveTax_AddsTaxOnTop()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var service = new TaxCalculationService(unitOfWork);

        var result = service.Calculate(1000m, taxEnabled: true, taxPercent: 15, taxInclusive: false);

        result.TaxAmount.Should().Be(150m);
        result.TotalAmount.Should().Be(1150m);
    }

    [Fact]
    public void Calculate_InclusiveTax_ExtractsTaxFromBaseWithoutAddingMore()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var service = new TaxCalculationService(unitOfWork);

        // 115 already includes 15% tax => pre-tax was 100, tax portion is 15.
        var result = service.Calculate(115m, taxEnabled: true, taxPercent: 15, taxInclusive: true);

        result.TaxAmount.Should().Be(15m);
        result.TotalAmount.Should().Be(115m, "inclusive tax doesn't change the amount the customer pays");
    }

    [Fact]
    public void Calculate_ZeroOrNegativeBase_ReturnsZeroTax()
    {
        var (_, unitOfWork) = TestUnitOfWorkFactory.Create();
        var service = new TaxCalculationService(unitOfWork);

        service.Calculate(0m, true, 15, false).TaxAmount.Should().Be(0m);
        service.Calculate(-50m, true, 15, false).TaxAmount.Should().Be(0m);
    }
}
