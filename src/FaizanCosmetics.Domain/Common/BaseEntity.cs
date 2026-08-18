namespace FaizanCosmetics.Domain.Common;

/// <summary>
/// Base class for all entities with an integer surrogate key and standard audit fields.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// Base class for entities that support soft-delete instead of physical deletion.
/// Used for entities that must be retained once financial transactions reference them
/// (e.g. Clients, Suppliers, Products).
/// </summary>
public abstract class SoftDeletableEntity : BaseEntity
{
    public bool IsActive { get; set; } = true;
}
