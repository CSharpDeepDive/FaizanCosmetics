using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Tests.Common;

/// <summary>
/// Builds a fresh, isolated in-memory ApplicationDbContext per test (unique database name per
/// call), pre-seeded with the one AppSetting row every repository/service expects to exist —
/// mirroring what DbInitializer guarantees in the real application.
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.AppSettings.Add(new AppSetting());
        context.SaveChanges();
        return context;
    }
}
