using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>The AppSetting table is single-row; this repository always returns/updates that one row.</summary>
public interface IAppSettingRepository
{
    Task<AppSetting> GetAsync(CancellationToken cancellationToken = default);
    void Update(AppSetting settings);
}
