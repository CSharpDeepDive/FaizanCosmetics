using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class SupplierEditViewModel : ViewModelBase
{
    private readonly ISupplierService _supplierService;
    private readonly ICurrentUserService _currentUser;

    public SupplierEditViewModel(ISupplierService supplierService, ICurrentUserService currentUser)
    {
        _supplierService = supplierService;
        _currentUser = currentUser;
    }

    [ObservableProperty] private int? supplierId;
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private string windowTitle = "Add Supplier";

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? contactPerson;
    [ObservableProperty] private decimal openingBalance;
    [ObservableProperty] private decimal currentBalance;

    public bool SavedSuccessfully { get; private set; }
    public event Action? RequestClose;

    public async Task InitializeAsync(int? existingSupplierId)
    {
        SupplierId = existingSupplierId;
        IsEditMode = existingSupplierId.HasValue;
        WindowTitle = IsEditMode ? "Edit Supplier" : "Add Supplier";

        if (IsEditMode)
        {
            var supplier = await _supplierService.GetByIdAsync(existingSupplierId!.Value);
            if (supplier is null)
            {
                ErrorMessage = "Supplier not found.";
                return;
            }

            Name = supplier.Name;
            Phone = supplier.Phone;
            Address = supplier.Address;
            Email = supplier.Email;
            ContactPerson = supplier.ContactPerson;
            CurrentBalance = supplier.Balance;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var currentUserId = _currentUser.UserId ?? throw new ValidationAppException("No user is signed in.");

            if (IsEditMode)
            {
                await _supplierService.UpdateAsync(new UpdateSupplierDto
                {
                    Id = SupplierId!.Value,
                    Name = Name,
                    Phone = Phone,
                    Address = Address,
                    Email = Email,
                    ContactPerson = ContactPerson
                }, currentUserId);
            }
            else
            {
                await _supplierService.CreateAsync(new CreateSupplierDto
                {
                    Name = Name,
                    Phone = Phone,
                    Address = Address,
                    Email = Email,
                    ContactPerson = ContactPerson,
                    OpeningBalance = OpeningBalance
                }, currentUserId);
            }

            SavedSuccessfully = true;
            RequestClose?.Invoke();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to save the supplier right now. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
