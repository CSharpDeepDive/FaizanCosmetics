using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class ReceivePaymentWindow : Window
{
    private readonly ReceivePaymentViewModel _viewModel;

    public ReceivePaymentWindow(ReceivePaymentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => DialogResult = _viewModel.SavedSuccessfully;
    }

    public void Initialize(int clientId)
    {
        _ = _viewModel.InitializeAsync(clientId);
    }
}
