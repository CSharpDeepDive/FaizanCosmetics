using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.UI.ViewModels;

public partial class SupplierPaymentViewModel : ViewModelBase
{
    private readonly ISupplierService _supplierService;
    private readonly ISupplierPaymentService _supplierPaymentService;
    private readonly ICurrentUserService _currentUser;
    private int _supplierId;

    public SupplierPaymentViewModel(ISupplierService supplierService, ISupplierPaymentService supplierPaymentService, ICurrentUserService currentUser)
    {
        _supplierService = supplierService;
        _supplierPaymentService = supplierPaymentService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = new[] { PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.BankTransfer };

    public bool SavedSuccessfully { get; private set; }
    public event Action? RequestClose;

    [ObservableProperty] private string supplierName = string.Empty;
    [ObservableProperty] private decimal currentBalance;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private PaymentMethod selectedPaymentMethod = PaymentMethod.Cash;
    [ObservableProperty] private string? referenceNumber;
    [ObservableProperty] private string? notes;

    public async Task InitializeAsync(int supplierId)
    {
        _supplierId = supplierId;
        var supplier = await _supplierService.GetByIdAsync(supplierId);
        if (supplier is null)
        {
            ErrorMessage = "Supplier not found.";
            return;
        }
        SupplierName = supplier.Name;
        CurrentBalance = supplier.Balance;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var currentUserId = _currentUser.UserId ?? throw new ValidationAppException("No user is signed in.");

            await _supplierPaymentService.PaySupplierAsync(new PaySupplierDto
            {
                SupplierId = _supplierId,
                Amount = Amount,
                PaymentMethod = SelectedPaymentMethod,
                ReferenceNumber = ReferenceNumber,
                Notes = Notes
            }, currentUserId);

            SavedSuccessfully = true;
            RequestClose?.Invoke();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to record the payment right now. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
