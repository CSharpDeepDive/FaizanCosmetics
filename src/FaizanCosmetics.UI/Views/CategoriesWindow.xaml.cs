using System.Windows;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

public partial class CategoriesWindow : Window
{
    public CategoriesWindow(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
