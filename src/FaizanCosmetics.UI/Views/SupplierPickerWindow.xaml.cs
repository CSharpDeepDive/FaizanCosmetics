using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class SupplierPickerWindow : Window
{
    private readonly SupplierPickerViewModel _viewModel;

    public SupplierPickerWindow(SupplierPickerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => DialogResult = _viewModel.Confirmed;
        Loaded += (_, _) => SearchBox.Focus();
    }

    public int? SelectedSupplierId => _viewModel.Confirmed ? _viewModel.SelectedSupplier?.Id : null;
    public string? SelectedSupplierName => _viewModel.Confirmed ? _viewModel.SelectedSupplier?.Name : null;

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedSupplier is not null)
        {
            _viewModel.SelectCommand.Execute(null);
        }
    }
}
