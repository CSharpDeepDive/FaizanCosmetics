using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class KhataStatementWindow : Window
{
    private readonly KhataStatementViewModel _viewModel;

    public KhataStatementWindow(KhataStatementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public void Initialize(int clientId)
    {
        _ = _viewModel.InitializeAsync(clientId);
    }
}
