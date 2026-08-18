namespace FaizanCosmetics.Application.DTOs;

public class DashboardSummaryDto
{
    public decimal TodaySales { get; set; }
    public decimal TodayProfit { get; set; }
    public int TodayInvoiceCount { get; set; }

    public decimal PendingClientDues { get; set; }
    public decimal SupplierOutstanding { get; set; }

    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }

    public decimal TodayCash { get; set; }
    public decimal TodayCard { get; set; }
    public decimal TodayBankTransfer { get; set; }

    public List<DailySalesPointDto> Last7DaysSales { get; set; } = new();
    public List<CategorySalesPointDto> SalesByCategory { get; set; } = new();
    public List<TopProductDto> TopSellingProducts { get; set; } = new();
}

public class DailySalesPointDto
{
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
}

public class CategorySalesPointDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
}

public class TopProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
