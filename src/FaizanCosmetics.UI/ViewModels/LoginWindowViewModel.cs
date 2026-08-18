using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class LoginWindowViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    public LoginWindowViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string username = string.Empty;

    /// <summary>The plaintext password itself is intentionally not bound here — WPF PasswordBox
    /// doesn't support safe two-way binding, so the code-behind reads it directly on submit.</summary>
    [ObservableProperty]
    private bool loginSucceeded;

    [ObservableProperty]
    private bool mustChangePassword;

    [ObservableProperty]
    private int? loggedInUserId;

    [RelayCommand]
    private async Task LoginAsync(string password)
    {
        ErrorMessage = null;
        LoginSucceeded = false;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Please enter both username and password.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(Username, password);
            if (result is null)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            LoggedInUserId = result.UserId;
            MustChangePassword = result.MustChangePassword;
            LoginSucceeded = true;
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to sign in right now. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
