using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public CategoryService(IUnitOfWork unitOfWork, IAuditService auditService, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public async Task<List<CategoryDto>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(activeOnly, cancellationToken);
        var result = new List<CategoryDto>(categories.Count);
        foreach (var category in categories)
        {
            result.Add(new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                ProductCount = await _unitOfWork.Categories.GetProductCountAsync(category.Id, cancellationToken)
            });
        }
        return result;
    }

    public async Task<int> CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        name = RequireName(name);

        if (await _unitOfWork.Categories.NameExistsAsync(name, null, cancellationToken))
        {
            throw new ValidationAppException($"A category named '{name}' already exists.");
        }

        var category = new Category { Name = name, Description = description, IsActive = true };
        _unitOfWork.Categories.Add(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await Audit("CategoryCreated", category.Id, $"Created category '{category.Name}'.", cancellationToken);
        return category.Id;
    }

    public async Task UpdateAsync(int id, string name, string? description, CancellationToken cancellationToken = default)
    {
        name = RequireName(name);

        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken)
            ?? throw new ValidationAppException("Category not found.");

        if (await _unitOfWork.Categories.NameExistsAsync(name, id, cancellationToken))
        {
            throw new ValidationAppException($"A category named '{name}' already exists.");
        }

        var oldName = category.Name;
        category.Name = name;
        category.Description = description;
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await Audit("CategoryUpdated", category.Id, $"Renamed category '{oldName}' to '{name}'.", cancellationToken);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken)
            ?? throw new ValidationAppException("Category not found.");

        var productCount = await _unitOfWork.Categories.GetProductCountAsync(id, cancellationToken);
        if (productCount > 0)
        {
            throw new ValidationAppException($"Cannot deactivate '{category.Name}' — {productCount} product(s) still use this category. Reassign them first.");
        }

        category.IsActive = false;
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await Audit("CategoryDeactivated", category.Id, $"Deactivated category '{category.Name}'.", cancellationToken);
    }

    private Task Audit(string action, int categoryId, string description, CancellationToken cancellationToken) =>
        _auditService.LogAsync(_currentUser.UserId ?? 0, action, "Category", categoryId, null, null, description, cancellationToken);

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationAppException("Category name is required.");
        }
        return name.Trim();
    }
}
