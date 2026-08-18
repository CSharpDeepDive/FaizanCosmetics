using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

public interface IClientService
{
    Task<(List<ClientListItemDto> Items, int TotalCount)> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ClientDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the client and, if OpeningBalance != 0, posts an OpeningBalance ledger entry — both atomically.</summary>
    Task<int> CreateAsync(CreateClientDto dto, int currentUserId, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateClientDto dto, int currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deactivates (IsActive = false). Never physically deletes. Throws if the client is the system Walk-in Customer, which can never be deactivated.</summary>
    Task DeactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
    Task ReactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
}
