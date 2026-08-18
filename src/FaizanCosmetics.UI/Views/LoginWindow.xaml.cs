using System.Windows;
using System.Windows.Input;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.UI;
using FaizanCosmetics.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FaizanCosmetics.UI.Views;

/// <summary>
/// Code-behind is limited to what MVVM genuinely cannot express safely: reading PasswordBox.Password
/// (which cannot be data-bound without holding the plaintext in a bindable property) and window
/// navigation on successful login.
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginWindowViewModel _viewModel;

    public LoginWindow(LoginWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += (_, _) => UsernameTextBox.Focus();
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        await AttemptLoginAsync();
    }

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await AttemptLoginAsync();
        }
    }

    private async Task AttemptLoginAsync()
    {
        await _viewModel.LoginCommand.ExecuteAsync(PasswordBox.Password);
        PasswordBox.Clear();

        if (!_viewModel.LoginSucceeded)
        {
            return;
        }

        if (_viewModel.MustChangePassword)
        {
            var changed = PromptForcedPasswordChange();
            if (!changed)
            {
                // User declined to set a new password — stay logged out rather than let them
                // into the app with the seeded default credential still active.
                _viewModel.LoginSucceeded = false;
                return;
            }
        }

        var mainWindow = App.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        Close();
    }

    private bool PromptForcedPasswordChange()
    {
        var dialog = new ChangePasswordWindow(_viewModel.LoggedInUserId!.Value, isForced: true) { Owner = this };
        var result = dialog.ShowDialog();
        return result == true;
    }
}
