namespace FaizanCosmetics.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(int userId, string action, string entity, int? entityId, string? oldValue, string? newValue, string? description, CancellationToken cancellationToken = default);
}
