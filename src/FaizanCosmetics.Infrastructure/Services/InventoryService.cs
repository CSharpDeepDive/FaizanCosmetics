using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.Infrastructure.Data;

namespace FaizanCosmetics.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private static readonly HashSet<InventoryTransactionType> IncreaseTypes = new()
    {
        InventoryTransactionType.Purchase,
        InventoryTransactionType.SaleReturn,
        InventoryTransactionType.AdjustmentIncrease,
        InventoryTransactionType.OpeningStock
    };

    private readonly ApplicationDbContext _context;
    private readonly IAppSettingRepository _appSettingRepository;

    public InventoryService(ApplicationDbContext context, IAppSettingRepository appSettingRepository)
    {
        _context = context;
        _appSettingRepository = appSettingRepository;
    }

    public bool IsIncreaseType(InventoryTransactionType transactionType) => IncreaseTypes.Contains(transactionType);

    public async Task<InventoryTransaction> PostTransactionAsync(
        Product product,
        InventoryTransactionType transactionType,
        decimal quantity,
        decimal unitCost,
        ReferenceType referenceType,
        int referenceId,
        int userId,
        string? notes = null,
        int? batchId = null,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ValidationAppException("Inventory transaction quantity must be greater than zero.");
        }

        var previousStock = product.CurrentStock;
        var isIncrease = IsIncreaseType(transactionType);
        var newStock = isIncrease ? previousStock + quantity : previousStock - quantity;

        if (!isIncrease && newStock < 0)
        {
            var settings = await _appSettingRepository.GetAsync(cancellationToken);
            if (!settings.AllowNegativeStock)
            {
                throw new InsufficientStockException(product.Name, previousStock, quantity);
            }
        }

        product.CurrentStock = newStock;

        var transaction = new InventoryTransaction
        {
            ProductId = product.Id,
            Product = product,
            TransactionType = transactionType,
            Quantity = quantity,
            PreviousStock = previousStock,
            NewStock = newStock,
            UnitCost = unitCost,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            BatchId = batchId,
            UserId = userId,
            TransactionDate = DateTime.UtcNow,
            Notes = notes
        };

        _context.InventoryTransactions.Add(transaction);

        // Intentionally no SaveChangesAsync here — this method only stages the change. The
        // caller (e.g. ProductService for opening stock, or a future SalesInvoiceService for a
        // sale) decides when the surrounding unit of work is committed, so this posting can
        // participate in a larger atomic operation.
        return transaction;
    }
}
