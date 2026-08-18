using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupplierLedgerService _supplierLedgerService;
    private readonly IAuditService _auditService;

    public SupplierService(IUnitOfWork unitOfWork, ISupplierLedgerService supplierLedgerService, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _supplierLedgerService = supplierLedgerService;
        _auditService = auditService;
    }

    public async Task<(List<SupplierListItemDto> Items, int TotalCount)> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (suppliers, total) = await _unitOfWork.Suppliers.SearchAsync(searchText, pageNumber, pageSize, cancellationToken);
        var balances = await _unitOfWork.SupplierLedgers.GetBalancesAsync(suppliers.Select(s => s.Id), cancellationToken);

        var items = suppliers.Select(s => new SupplierListItemDto
        {
            Id = s.Id,
            Name = s.Name,
            Phone = s.Phone,
            ContactPerson = s.ContactPerson,
            Balance = balances.GetValueOrDefault(s.Id),
            IsActive = s.IsActive
        }).ToList();

        return (items, total);
    }

    public async Task<SupplierDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id, cancellationToken);
        if (supplier is null) return null;

        var balance = await _supplierLedgerService.GetBalanceAsync(id, cancellationToken);

        return new SupplierDetailDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Phone = supplier.Phone,
            Address = supplier.Address,
            Email = supplier.Email,
            ContactPerson = supplier.ContactPerson,
            Balance = balance,
            IsActive = supplier.IsActive
        };
    }

    public async Task<int> CreateAsync(CreateSupplierDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        Validate(dto.Name);

        var supplier = new Supplier
        {
            Name = dto.Name.Trim(),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Address = dto.Address,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            ContactPerson = dto.ContactPerson,
            OpeningBalance = dto.OpeningBalance,
            IsActive = true
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _unitOfWork.Suppliers.Add(supplier);
            await _unitOfWork.SaveChangesAsync(ct);

            if (dto.OpeningBalance != 0)
            {
                await _supplierLedgerService.PostEntryAsync(
                    supplier.Id, SupplierLedgerEntryType.OpeningBalance, ReferenceType.OpeningBalance, supplier.Id,
                    debit: dto.OpeningBalance < 0 ? -dto.OpeningBalance : 0,
                    credit: dto.OpeningBalance > 0 ? dto.OpeningBalance : 0,
                    currentUserId, "Opening balance recorded at supplier creation.", ct);
            }
        }, cancellationToken);

        await _auditService.LogAsync(currentUserId, "SupplierCreated", "Supplier", supplier.Id, null, null, $"Created supplier '{supplier.Name}'.", cancellationToken);
        return supplier.Id;
    }

    public async Task UpdateAsync(UpdateSupplierDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        Validate(dto.Name);

        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new ValidationAppException("Supplier not found.");

        supplier.Name = dto.Name.Trim();
        supplier.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        supplier.Address = dto.Address;
        supplier.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        supplier.ContactPerson = dto.ContactPerson;

        _unitOfWork.Suppliers.Update(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, "SupplierUpdated", "Supplier", supplier.Id, null, null, $"Updated supplier '{supplier.Name}'.", cancellationToken);
    }

    public async Task DeactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id, cancellationToken)
            ?? throw new ValidationAppException("Supplier not found.");

        supplier.IsActive = false;
        _unitOfWork.Suppliers.Update(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, "SupplierDeactivated", "Supplier", supplier.Id, null, null, $"Deactivated supplier '{supplier.Name}'.", cancellationToken);
    }

    public async Task ReactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id, cancellationToken)
            ?? throw new ValidationAppException("Supplier not found.");

        supplier.IsActive = true;
        _unitOfWork.Suppliers.Update(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, "SupplierReactivated", "Supplier", supplier.Id, null, null, $"Reactivated supplier '{supplier.Name}'.", cancellationToken);
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationAppException("Supplier name is required.");
    }
}
