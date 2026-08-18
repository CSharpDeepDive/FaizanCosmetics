using System.Windows;
using System.Windows.Controls;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class ClientPickerWindow : Window
{
    private readonly ClientPickerViewModel _viewModel;

    public ClientPickerWindow(ClientPickerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => DialogResult = _viewModel.Confirmed;
        Loaded += (_, _) => SearchBox.Focus();
    }

    /// <summary>Null if the dialog was cancelled.</summary>
    public int? SelectedClientId => _viewModel.Confirmed ? _viewModel.SelectedClient?.Id : null;
    public string? SelectedClientName => _viewModel.Confirmed ? _viewModel.SelectedClient?.Name : null;

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedClient is not null)
        {
            _viewModel.SelectCommand.Execute(null);
        }
    }
}
