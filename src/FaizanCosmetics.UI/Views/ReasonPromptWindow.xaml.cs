using System.Windows;

namespace FaizanCosmetics.UI.Views;

/// <summary>
/// A small reusable confirm-with-reason dialog for destructive/financially significant actions
/// that need an audit trail entry explaining why (spec §60's "require confirmation for
/// destructive operations" list). No ViewModel — this is a pure, stateless prompt, so plain
/// code-behind is the simplest honest implementation rather than manufacturing MVVM ceremony
/// around two text properties and two buttons.
/// </summary>
public partial class ReasonPromptWindow : Window
{
    public ReasonPromptWindow()
    {
        InitializeComponent();
    }

    public string? EnteredReason { get; private set; }

    public void Initialize(string title, string message)
    {
        Title = title;
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
        {
            MessageTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
            MessageTextBlock.Text = "Please enter a reason to continue.";
            return;
        }
        EnteredReason = ReasonTextBox.Text.Trim();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
