using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class ProductPickerWindow : Window
{
    private readonly ProductPickerViewModel _viewModel;

    public ProductPickerWindow(ProductPickerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => DialogResult = _viewModel.Confirmed;
        Loaded += (_, _) => SearchBox.Focus();
    }

    public int? SelectedProductId => _viewModel.Confirmed ? _viewModel.SelectedProduct?.Id : null;

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedProduct is not null)
        {
            _viewModel.SelectCommand.Execute(null);
        }
    }
}
