using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.UI.ViewModels;

public partial class ClientEditViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUser;

    public ClientEditViewModel(IClientService clientService, ICurrentUserService currentUser)
    {
        _clientService = clientService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<ClientType> ClientTypes { get; } = Enum.GetValues<ClientType>();

    [ObservableProperty] private int? clientId;
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private bool isWalkInCustomer;
    [ObservableProperty] private string windowTitle = "Add Client";
    [ObservableProperty] private string clientCode = "(assigned on save)";

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? email;
    [ObservableProperty] private ClientType selectedClientType = ClientType.Retail;
    [ObservableProperty] private decimal creditLimit;
    [ObservableProperty] private decimal openingBalance;
    [ObservableProperty] private decimal currentBalance;

    public bool SavedSuccessfully { get; private set; }
    public event Action? RequestClose;

    public async Task InitializeAsync(int? existingClientId)
    {
        ClientId = existingClientId;
        IsEditMode = existingClientId.HasValue;
        WindowTitle = IsEditMode ? "Edit Client" : "Add Client";

        if (IsEditMode)
        {
            var client = await _clientService.GetByIdAsync(existingClientId!.Value);
            if (client is null)
            {
                ErrorMessage = "Client not found.";
                return;
            }

            ClientCode = client.ClientCode;
            IsWalkInCustomer = client.IsWalkInCustomer;
            Name = client.Name;
            Phone = client.Phone;
            Address = client.Address;
            Email = client.Email;
            SelectedClientType = client.ClientType;
            CreditLimit = client.CreditLimit;
            CurrentBalance = client.Balance;
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
                await _clientService.UpdateAsync(new UpdateClientDto
                {
                    Id = ClientId!.Value,
                    Name = Name,
                    Phone = Phone,
                    Address = Address,
                    Email = Email,
                    ClientType = SelectedClientType,
                    CreditLimit = CreditLimit
                }, currentUserId);
            }
            else
            {
                await _clientService.CreateAsync(new CreateClientDto
                {
                    Name = Name,
                    Phone = Phone,
                    Address = Address,
                    Email = Email,
                    ClientType = SelectedClientType,
                    CreditLimit = CreditLimit,
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
            ErrorMessage = "Unable to save the client right now. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
