using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;
    public DashboardRepository(ApplicationDbContext context) => _context = context;

    public async Task<DashboardSummaryDto> GetSummaryAsync(int lastDaysForChart = 7, int topProductCount = 5, CancellationToken cancellationToken = default)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var postedInvoices = _context.SalesInvoices.AsNoTracking().Where(i => i.Status == InvoiceStatus.Posted);
        var todayInvoices = postedInvoices.Where(i => i.InvoiceDate >= todayStart && i.InvoiceDate < todayEnd);

        var summary = new DashboardSummaryDto
        {
            TodaySales = await todayInvoices.SumAsync(i => (decimal?)i.GrandTotal, cancellationToken) ?? 0m,
            TodayInvoiceCount = await todayInvoices.CountAsync(cancellationToken),
            PendingClientDues = await postedInvoices.SumAsync(i => (decimal?)i.DueAmount, cancellationToken) ?? 0m,
            SupplierOutstanding = await _context.PurchaseInvoices.AsNoTracking()
                .Where(p => p.Status == InvoiceStatus.Posted)
                .SumAsync(p => (decimal?)p.DueAmount, cancellationToken) ?? 0m,
            LowStockCount = await _context.Products.AsNoTracking()
                .CountAsync(p => p.IsActive && p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStockLevel, cancellationToken),
            OutOfStockCount = await _context.Products.AsNoTracking()
                .CountAsync(p => p.IsActive && p.CurrentStock <= 0, cancellationToken)
        };

        // Today's profit: (line revenue net of tax) minus (cost of goods sold), computed from the
        // immutable per-line snapshots so it reflects the cost that actually applied at sale time.
        var todayItemFigures = await _context.SalesInvoiceItems.AsNoTracking()
            .Where(it => it.SalesInvoice.Status == InvoiceStatus.Posted
                      && it.SalesInvoice.InvoiceDate >= todayStart && it.SalesInvoice.InvoiceDate < todayEnd)
            .Select(it => new { it.LineTotal, it.TaxAmount, CostOfGoods = it.Quantity * it.UnitCostSnapshot })
            .ToListAsync(cancellationToken);
        summary.TodayProfit = todayItemFigures.Sum(x => x.LineTotal - x.TaxAmount - x.CostOfGoods);

        var todayPaymentTotals = await todayInvoices
            .GroupBy(i => i.PaymentMethod)
            .Select(g => new { Method = g.Key, Total = g.Sum(i => i.PaidAmount) })
            .ToListAsync(cancellationToken);
        summary.TodayCash = todayPaymentTotals.FirstOrDefault(x => x.Method == PaymentMethod.Cash)?.Total ?? 0m;
        summary.TodayCard = todayPaymentTotals.FirstOrDefault(x => x.Method == PaymentMethod.Card)?.Total ?? 0m;
        summary.TodayBankTransfer = todayPaymentTotals.FirstOrDefault(x => x.Method == PaymentMethod.BankTransfer)?.Total ?? 0m;

        var chartStart = todayStart.AddDays(-(lastDaysForChart - 1));
        var rawDaily = await postedInvoices
            .Where(i => i.InvoiceDate >= chartStart && i.InvoiceDate < todayEnd)
            .GroupBy(i => i.InvoiceDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(i => i.GrandTotal) })
            .ToListAsync(cancellationToken);

        // Fill every day in the window explicitly (including zero-sales days) so the 7-day chart
        // never silently skips a day just because nothing sold.
        summary.Last7DaysSales = Enumerable.Range(0, lastDaysForChart)
            .Select(offset => chartStart.AddDays(offset))
            .Select(date => new DailySalesPointDto
            {
                Date = date,
                TotalSales = rawDaily.FirstOrDefault(r => r.Date == date)?.Total ?? 0m
            })
            .ToList();

        summary.SalesByCategory = await _context.SalesInvoiceItems.AsNoTracking()
            .Where(it => it.SalesInvoice.Status == InvoiceStatus.Posted)
            .Select(it => new { it.LineTotal, it.Product.CategoryId, it.Product.Category.Name })
            .GroupBy(x => new { x.CategoryId, x.Name })
            .Select(g => new CategorySalesPointDto { CategoryName = g.Key.Name, TotalSales = g.Sum(x => x.LineTotal) })
            .OrderByDescending(x => x.TotalSales)
            .ToListAsync(cancellationToken);

        summary.TopSellingProducts = await _context.SalesInvoiceItems.AsNoTracking()
            .Where(it => it.SalesInvoice.Status == InvoiceStatus.Posted)
            .GroupBy(it => it.ProductNameSnapshot)
            .Select(g => new TopProductDto
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(topProductCount)
            .ToListAsync(cancellationToken);

        return summary;
    }
}
