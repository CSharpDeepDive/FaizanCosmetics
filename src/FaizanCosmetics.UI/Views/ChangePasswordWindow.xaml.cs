using System.Windows;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.UI;
using Microsoft.Extensions.DependencyInjection;

namespace FaizanCosmetics.UI.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly int _userId;
    private readonly bool _isForced;
    private readonly IAuthService _authService;

    public ChangePasswordWindow(int userId, bool isForced)
    {
        InitializeComponent();
        _userId = userId;
        _isForced = isForced;
        _authService = App.Services.GetRequiredService<IAuthService>();

        if (isForced)
        {
            HeaderTextBlock.Text = "You're using a default password. Please set a new one to continue.";
            CancelButton.Visibility = Visibility.Collapsed;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Visibility = Visibility.Collapsed;

        var current = CurrentPasswordBox.Password;
        var newPassword = NewPasswordBox.Password;
        var confirm = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPassword))
        {
            ShowError("Please fill in all fields.");
            return;
        }

        if (newPassword != confirm)
        {
            ShowError("New password and confirmation do not match.");
            return;
        }

        try
        {
            await _authService.ChangePasswordAsync(_userId, current, newPassword);
            DialogResult = true;
        }
        catch (AppException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception)
        {
            ShowError("Unable to change password right now. Please try again.");
        }
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}
