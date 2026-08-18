namespace FaizanCosmetics.Application.Interfaces;

public readonly record struct TaxCalculationResult(decimal TaxAmount, decimal TotalAmount);

/// <summary>
/// The single place tax math happens, so it's never duplicated across ViewModels or services
/// (per spec §13). Reads AppSettings' TaxEnabled/DefaultTaxPercent/TaxInclusivePricing so callers
/// never need to branch on those flags themselves.
/// </summary>
public interface ITaxCalculationService
{
    /// <summary>
    /// Computes tax on <paramref name="baseAmount"/>. If tax is disabled in settings, always
    /// returns TaxAmount = 0 and TotalAmount = baseAmount. If TaxInclusivePricing is true,
    /// baseAmount is treated as already including tax (the tax is extracted, not added).
    /// </summary>
    Task<TaxCalculationResult> CalculateAsync(decimal baseAmount, CancellationToken cancellationToken = default);

    /// <summary>Synchronous variant for callers that already have the current tax settings loaded (e.g. a loop processing many invoice lines) — avoids re-querying AppSettings per line.</summary>
    TaxCalculationResult Calculate(decimal baseAmount, bool taxEnabled, decimal taxPercent, bool taxInclusive);
}
