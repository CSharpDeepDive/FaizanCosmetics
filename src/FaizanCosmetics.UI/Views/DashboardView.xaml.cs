using System.Windows.Controls;

namespace FaizanCosmetics.UI.Views;

/// <summary>
/// Parameterless by design: this View is instantiated by WPF's implicit DataTemplate lookup
/// (see MainWindow.xaml), not resolved through DI. Its DataContext is never set explicitly here —
/// it inherits automatically from the DataTemplate's data object (the DashboardViewModel instance
/// that INavigationService placed in MainWindowViewModel.CurrentView), which is the standard WPF
/// mechanism for MVVM navigation and avoids ever constructing two different ViewModel instances.
/// </summary>
public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }
}
