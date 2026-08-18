using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>
/// Read-only aggregation queries for the dashboard. Deliberately separate from the transactional
/// repositories in IUnitOfWork — this never participates in a write transaction, so it can be
/// safely cached/short-circuited or later moved behind a materialized view without touching
/// business logic elsewhere.
/// </summary>
public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(int lastDaysForChart = 7, int topProductCount = 5, CancellationToken cancellationToken = default);
}
