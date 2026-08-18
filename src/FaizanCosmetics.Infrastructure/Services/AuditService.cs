using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;

namespace FaizanCosmetics.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int userId, string action, string entity, int? entityId, string? oldValue, string? newValue, string? description, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Description = description,
            DateTime = DateTime.UtcNow
        });

        // Audit rows are saved immediately and independently of the caller's own SaveChanges,
        // so an audit entry is never lost if the surrounding business transaction rolls back
        // for an unrelated reason after the audited action already succeeded.
        await _context.SaveChangesAsync(cancellationToken);
    }
}
