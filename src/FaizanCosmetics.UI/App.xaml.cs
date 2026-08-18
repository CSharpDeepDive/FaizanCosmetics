using System.IO;
using System.Windows;
using System.Windows.Threading;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Infrastructure;
using FaizanCosmetics.Infrastructure.Data;
using FaizanCosmetics.UI.Services;
using FaizanCosmetics.UI.ViewModels;
using FaizanCosmetics.UI.Views;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FaizanCosmetics.UI;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    public static IServiceProvider Services =>
        ((App)Current)._serviceProvider ?? throw new InvalidOperationException("Service provider is not initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var basePath = AppContext.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var logDirectory = Path.Combine(basePath, configuration["Logging:LogDirectory"] ?? "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logDirectory, "faizancosmetics-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: int.TryParse(configuration["Logging:RetainedFileCountLimit"], out var n) ? n : 30,
                // Full exception details go to the log file only — never to a user-facing dialog,
                // which must stay generic (see DispatcherUnhandledException below).
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddSerilog(dispose: true));
        services.AddInfrastructure(configuration);
        RegisterUi(services);

        _serviceProvider = services.BuildServiceProvider();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        try
        {
            using (var scope = Services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
                await initializer.InitializeAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database initialization failed at startup.");
            MessageBox.Show(
                "Unable to connect to the database. Please verify SQL Server is running and the connection string in appsettings.json is correct, then restart the application.",
                "Faizan Cosmetics — Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private static void RegisterUi(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<LoginWindowViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PlaceholderViewModel>();

        services.AddTransient<ProductsViewModel>();
        services.AddTransient<LowStockViewModel>();
        services.AddTransient<ProductEditViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<PriceHistoryViewModel>();

        services.AddTransient<ProductEditWindow>();
        services.AddTransient<CategoriesWindow>();
        services.AddTransient<PriceHistoryWindow>();

        services.AddTransient<ClientsViewModel>();
        services.AddTransient<ClientEditViewModel>();
        services.AddTransient<KhataStatementViewModel>();
        services.AddTransient<ReceivePaymentViewModel>();

        services.AddTransient<ClientEditWindow>();
        services.AddTransient<KhataStatementWindow>();
        services.AddTransient<ReceivePaymentWindow>();

        services.AddTransient<SalesInvoiceViewModel>();
        services.AddTransient<SalesHistoryViewModel>();
        services.AddTransient<ClientPickerViewModel>();
        services.AddTransient<ProductPickerViewModel>();

        services.AddTransient<ClientPickerWindow>();
        services.AddTransient<ProductPickerWindow>();
        services.AddTransient<ReasonPromptWindow>();

        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<SupplierEditViewModel>();
        services.AddTransient<SupplierStatementViewModel>();
        services.AddTransient<SupplierPaymentViewModel>();
        services.AddTransient<SupplierPickerViewModel>();
        services.AddTransient<PurchaseInvoiceViewModel>();
        services.AddTransient<PurchaseHistoryViewModel>();

        services.AddTransient<SupplierEditWindow>();
        services.AddTransient<SupplierStatementWindow>();
        services.AddTransient<SupplierPaymentWindow>();
        services.AddTransient<SupplierPickerWindow>();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception.");
        MessageBox.Show(
            "Something went wrong and the last action could not be completed. The technical details have been logged. Please try again.",
            "Faizan Cosmetics", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Fatal(ex, "Unhandled non-UI-thread exception. Application will terminate.");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
