using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.UI.Services;
using FaizanCosmetics.UI.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace FaizanCosmetics.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    public DashboardViewModel(IDashboardRepository dashboardRepository, IAppSettingRepository appSettingRepository, INavigationService navigationService, IServiceProvider serviceProvider)
    {
        _dashboardRepository = dashboardRepository;
        _appSettingRepository = appSettingRepository;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;

        SalesTrendSeries = new ObservableCollection<ISeries>();
        SalesTrendXAxes = new ObservableCollection<LiveChartsCore.SkiaSharpView.Axis> { new() { Labels = Array.Empty<string>() } };
        CategorySeries = new ObservableCollection<ISeries>();

        _ = LoadAsync();
    }

    [ObservableProperty] private string currencySymbol = "Rs.";
    [ObservableProperty] private decimal todaySales;
    [ObservableProperty] private decimal todayProfit;
    [ObservableProperty] private int todayInvoiceCount;
    [ObservableProperty] private decimal pendingClientDues;
    [ObservableProperty] private decimal supplierOutstanding;
    [ObservableProperty] private int lowStockCount;
    [ObservableProperty] private int outOfStockCount;
    [ObservableProperty] private decimal todayCash;
    [ObservableProperty] private decimal todayCard;
    [ObservableProperty] private decimal todayBankTransfer;

    public ObservableCollection<TopProductDto> TopSellingProducts { get; } = new();
    public ObservableCollection<ISeries> SalesTrendSeries { get; }
    public ObservableCollection<LiveChartsCore.SkiaSharpView.Axis> SalesTrendXAxes { get; }
    public ObservableCollection<ISeries> CategorySeries { get; }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void NewInvoice() => NavigateToPlaceholder("New Invoice", "Phase 5 — Sales Invoice & Payment Processing");

    [RelayCommand]
    private void ReceivePayment() => _navigationService.NavigateTo<ClientsViewModel>();

    [RelayCommand]
    private void AddClient()
    {
        var dialog = _serviceProvider.GetRequiredService<ClientEditWindow>();
        dialog.Initialize(clientId: null);
        if (dialog.ShowDialog() == true)
        {
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    private void AddProduct()
    {
        var dialog = _serviceProvider.GetRequiredService<ProductEditWindow>();
        dialog.Initialize(productId: null);
        if (dialog.ShowDialog() == true)
        {
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    private void Purchase() => _navigationService.NavigateTo<PurchaseInvoiceViewModel>();

    [RelayCommand]
    private void StockAdjustment() => NavigateToPlaceholder("Stock Adjustment", "Phase 7 — Returns & Inventory Adjustments");

    [RelayCommand]
    private void Reports() => NavigateToPlaceholder("Reports", "Phase 8 — Reports & Analytics");

    private void NavigateToPlaceholder(string moduleName, string plannedPhase) =>
        _navigationService.NavigateToInstance(new PlaceholderViewModel { ModuleName = moduleName, PlannedPhase = $"Coming in {plannedPhase}." });

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var settings = await _appSettingRepository.GetAsync();
            CurrencySymbol = settings.CurrencySymbol;

            var summary = await _dashboardRepository.GetSummaryAsync();
            Apply(summary);
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to load dashboard data. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Apply(DashboardSummaryDto summary)
    {
        TodaySales = summary.TodaySales;
        TodayProfit = summary.TodayProfit;
        TodayInvoiceCount = summary.TodayInvoiceCount;
        PendingClientDues = summary.PendingClientDues;
        SupplierOutstanding = summary.SupplierOutstanding;
        LowStockCount = summary.LowStockCount;
        OutOfStockCount = summary.OutOfStockCount;
        TodayCash = summary.TodayCash;
        TodayCard = summary.TodayCard;
        TodayBankTransfer = summary.TodayBankTransfer;

        TopSellingProducts.Clear();
        foreach (var product in summary.TopSellingProducts) TopSellingProducts.Add(product);

        SalesTrendXAxes[0].Labels = summary.Last7DaysSales.Select(p => p.Date.ToString("dd MMM")).ToArray();
        SalesTrendSeries.Clear();
        SalesTrendSeries.Add(new LineSeries<decimal>
        {
            Values = summary.Last7DaysSales.Select(p => p.TotalSales).ToArray(),
            Name = "Sales",
            Fill = null,
            GeometrySize = 6,
            Stroke = new SolidColorPaint(new SKColor(0x6B, 0x3F, 0xA0), 3)
        });

        CategorySeries.Clear();
        var palette = new[] { 0x6B3FA0, 0xE091C4, 0x2E9E5B, 0xD98E1E, 0xC63B3B, 0x3F7FA0, 0x9E7A2E };
        var index = 0;
        foreach (var category in summary.SalesByCategory)
        {
            var color = palette[index % palette.Length];
            CategorySeries.Add(new PieSeries<decimal>
            {
                Values = new[] { category.TotalSales },
                Name = category.CategoryName,
                Fill = new SolidColorPaint(new SKColor((byte)(color >> 16), (byte)(color >> 8), (byte)color))
            });
            index++;
        }
    }
}
