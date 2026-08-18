using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class PriceHistoryWindow : Window
{
    private readonly PriceHistoryViewModel _viewModel;

    public PriceHistoryWindow(PriceHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public void Initialize(int productId, string productName)
    {
        _ = _viewModel.InitializeAsync(productId, productName);
    }
}
