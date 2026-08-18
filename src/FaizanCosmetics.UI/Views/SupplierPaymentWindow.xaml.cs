using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class SupplierPaymentWindow : Window
{
    private readonly SupplierPaymentViewModel _viewModel;

    public SupplierPaymentWindow(SupplierPaymentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => DialogResult = _viewModel.SavedSuccessfully;
    }

    public void Initialize(int supplierId)
    {
        _ = _viewModel.InitializeAsync(supplierId);
    }
}
