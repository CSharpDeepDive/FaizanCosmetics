using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FaizanCosmetics.Infrastructure.Data;

/// <summary>
/// Applies pending EF Core migrations and seeds the minimum data the application requires to
/// start: the admin user, the Walk-in Customer, default categories, and the single AppSettings row.
/// Safe to call every application startup — every seed is idempotent (checked before insert).
/// </summary>
public class DbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<DbInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying pending database migrations...");
        await _context.Database.MigrateAsync(cancellationToken);

        await SeedAdminUserAsync(cancellationToken);
        await SeedWalkInCustomerAsync(cancellationToken);
        await SeedCategoriesAsync(cancellationToken);
        await SeedAppSettingsAsync(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Database initialization complete.");
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Username == "admin", cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding default admin user. The default password MUST be changed on first login.");
        _context.Users.Add(new User
        {
            Username = "admin",
            // Default password: Admin@123 — MustChangePassword forces an immediate change on
            // first login (see LoginWindowViewModel), so this is never usable beyond that.
            PasswordHash = _passwordHasher.Hash("Admin@123"),
            FullName = "System Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            MustChangePassword = true,
            MaxDiscountPercent = 100,
            CanOverrideCreditLimit = true
        });
    }

    private async Task SeedWalkInCustomerAsync(CancellationToken cancellationToken)
    {
        if (await _context.Clients.AnyAsync(c => c.IsWalkInCustomer, cancellationToken))
        {
            return;
        }

        _context.Clients.Add(new Client
        {
            ClientCode = "CL-000000",
            Name = "Walk-in Customer",
            ClientType = ClientType.Retail,
            CreditLimit = 0,
            OpeningBalance = 0,
            IsActive = true,
            IsWalkInCustomer = true,
            RegistrationDate = DateTime.UtcNow
        });
    }

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var names = new[] { "Hair Care", "Shaving", "Cosmetics", "Skin Care", "Perfumes", "Accessories", "Other" };
        foreach (var name in names)
        {
            _context.Categories.Add(new Category { Name = name, IsActive = true });
        }
    }

    private async Task SeedAppSettingsAsync(CancellationToken cancellationToken)
    {
        if (await _context.AppSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.AppSettings.Add(new AppSetting());
    }
}
