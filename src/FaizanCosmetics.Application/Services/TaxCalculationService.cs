using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.Application.Services;

public class TaxCalculationService : ITaxCalculationService
{
    private readonly IUnitOfWork _unitOfWork;

    public TaxCalculationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaxCalculationResult> CalculateAsync(decimal baseAmount, CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.AppSettings.GetAsync(cancellationToken);
        return Calculate(baseAmount, settings.TaxEnabled, settings.DefaultTaxPercent, settings.TaxInclusivePricing);
    }

    public TaxCalculationResult Calculate(decimal baseAmount, bool taxEnabled, decimal taxPercent, bool taxInclusive)
    {
        if (!taxEnabled || taxPercent <= 0 || baseAmount <= 0)
        {
            return new TaxCalculationResult(0m, baseAmount);
        }

        if (taxInclusive)
        {
            // baseAmount already contains the tax — extract it rather than adding more on top.
            // e.g. baseAmount = 115, taxPercent = 15% => tax = 115 - (115 / 1.15) = 15.
            var preTaxAmount = baseAmount / (1 + taxPercent / 100m);
            var extractedTax = baseAmount - preTaxAmount;
            return new TaxCalculationResult(Math.Round(extractedTax, 2), baseAmount);
        }

        var addedTax = Math.Round(baseAmount * taxPercent / 100m, 2);
        return new TaxCalculationResult(addedTax, baseAmount + addedTax);
    }
}
