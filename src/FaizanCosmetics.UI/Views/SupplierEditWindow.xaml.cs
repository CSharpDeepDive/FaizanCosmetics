using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class SupplierEditWindow : Window
{
    private readonly SupplierEditViewModel _viewModel;

    public SupplierEditWindow(SupplierEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => DialogResult = _viewModel.SavedSuccessfully;
    }

    public void Initialize(int? supplierId)
    {
        _ = _viewModel.InitializeAsync(supplierId);
    }
}
