using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class ProductEditWindow : Window
{
    private readonly ProductEditViewModel _viewModel;

    public ProductEditWindow(ProductEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += () => DialogResult = _viewModel.SavedSuccessfully;
        Loaded += (_, _) => BarcodeTextBox.Focus();
    }

    /// <summary>Called by the caller before ShowDialog(). Pass null to add a new product, or an
    /// existing product's Id to edit it. Loading is async but ShowDialog() blocks synchronously,
    /// so we fire the load and let the window open with an IsBusy spinner state rather than
    /// blocking the UI thread — WPF Windows can't easily await before ShowDialog in a modal flow.</summary>
    public void Initialize(int? productId)
    {
        _ = _viewModel.InitializeAsync(productId);
    }
}
