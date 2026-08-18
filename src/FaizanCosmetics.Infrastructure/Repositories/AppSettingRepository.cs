using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly ApplicationDbContext _context;
    public AppSettingRepository(ApplicationDbContext context) => _context = context;

    public async Task<AppSetting> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.AppSettings.FirstOrDefaultAsync(cancellationToken);
        return settings ?? throw new InvalidOperationException(
            "AppSettings row is missing. It should have been created by database seeding; re-run migrations/seeding to restore it.");
    }

    public void Update(AppSetting settings) => _context.AppSettings.Update(settings);
}
