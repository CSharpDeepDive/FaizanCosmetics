using System.Windows.Controls;
using System.Windows.Input;
using FaizanCosmetics.UI.ViewModels;

namespace FaizanCosmetics.UI.Views;

/// <summary>
/// Parameterless — instantiated via implicit DataTemplate; see DashboardView's comment for why.
/// F2 (focus barcode input) and F7 (focus payment amount) are handled here in code-behind rather
/// than as ViewModel commands, because moving keyboard focus to a specific control is exactly the
/// kind of UI-only behavior MVVM can't (and shouldn't) express — every other shortcut is a real
/// Command binding in the XAML (see UserControl.InputBindings).
/// </summary>
public partial class SalesInvoiceView : UserControl
{
    public SalesInvoiceView()
    {
        InitializeComponent();
        Loaded += (_, _) => BarcodeTextBox.Focus();
        PreviewKeyDown += SalesInvoiceView_PreviewKeyDown;
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is SalesInvoiceViewModel oldVm) oldVm.InvoicePosted -= OnInvoicePosted;
            if (e.NewValue is SalesInvoiceViewModel newVm) newVm.InvoicePosted += OnInvoicePosted;
        };
    }

    private void OnInvoicePosted(int invoiceId) => Dispatcher.BeginInvoke(() => BarcodeTextBox.Focus());

    private void SalesInvoiceView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2)
        {
            BarcodeTextBox.Focus();
            BarcodeTextBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.F7)
        {
            PaidAmountTextBox.Focus();
            PaidAmountTextBox.SelectAll();
            e.Handled = true;
        }
    }
}
