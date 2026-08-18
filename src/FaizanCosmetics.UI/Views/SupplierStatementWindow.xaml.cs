using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class SupplierStatementWindow : Window
{
    private readonly SupplierStatementViewModel _viewModel;

    public SupplierStatementWindow(SupplierStatementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public void Initialize(int supplierId)
    {
        _ = _viewModel.InitializeAsync(supplierId);
    }
}
