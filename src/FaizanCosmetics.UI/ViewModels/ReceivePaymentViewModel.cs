using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.UI.ViewModels;

public partial class ReceivePaymentViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly IClientPaymentService _clientPaymentService;
    private readonly ICurrentUserService _currentUser;
    private int _clientId;

    public ReceivePaymentViewModel(IClientService clientService, IClientPaymentService clientPaymentService, ICurrentUserService currentUser)
    {
        _clientService = clientService;
        _clientPaymentService = clientPaymentService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = new[] { PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.BankTransfer };

    public bool SavedSuccessfully { get; private set; }
    public event Action? RequestClose;

    [ObservableProperty] private string clientName = string.Empty;
    [ObservableProperty] private decimal currentBalance;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private PaymentMethod selectedPaymentMethod = PaymentMethod.Cash;
    [ObservableProperty] private string? referenceNumber;
    [ObservableProperty] private string? notes;

    public async Task InitializeAsync(int clientId)
    {
        _clientId = clientId;
        var client = await _clientService.GetByIdAsync(clientId);
        if (client is null)
        {
            ErrorMessage = "Client not found.";
            return;
        }
        ClientName = client.Name;
        CurrentBalance = client.Balance;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var currentUserId = _currentUser.UserId ?? throw new ValidationAppException("No user is signed in.");

            await _clientPaymentService.ReceivePaymentAsync(new ReceiveClientPaymentDto
            {
                ClientId = _clientId,
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
