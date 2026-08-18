using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.Tests.Common;

/// <summary>No-op audit logger for tests — avoids asserting on audit rows in tests that aren't about auditing.</summary>
public class TestAuditService : IAuditService
{
    public List<(int UserId, string Action, string Entity, int? EntityId, string? Description)> Logged { get; } = new();

    public Task LogAsync(int userId, string action, string entity, int? entityId, string? oldValue, string? newValue, string? description, CancellationToken cancellationToken = default)
    {
        Logged.Add((userId, action, entity, entityId, description));
        return Task.CompletedTask;
    }
}
