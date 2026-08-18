using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Application.Services;
using FaizanCosmetics.Infrastructure.Data;
using FaizanCosmetics.Infrastructure.Repositories;
using FaizanCosmetics.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FaizanCosmetics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in appsettings.json.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 3);
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            }));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientLedgerRepository, ClientLedgerRepository>();
        services.AddScoped<IClientPaymentRepository, ClientPaymentRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISupplierLedgerRepository, SupplierLedgerRepository>();
        services.AddScoped<ISupplierPaymentRepository, SupplierPaymentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
        services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Cross-cutting infrastructure services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IInventoryService, InventoryService>();

        // Application services that depend only on Application-layer interfaces are registered
        // here (rather than in the Application project) to keep Application free of a DI package
        // reference; this is the composition root's job.
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IClientLedgerService, ClientLedgerService>();
        services.AddScoped<IClientPaymentService, ClientPaymentService>();
        services.AddScoped<ITaxCalculationService, TaxCalculationService>();
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ISupplierLedgerService, SupplierLedgerService>();
        services.AddScoped<ISupplierPaymentService, SupplierPaymentService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();

        services.AddScoped<DbInitializer>();

        return services;
    }
}
