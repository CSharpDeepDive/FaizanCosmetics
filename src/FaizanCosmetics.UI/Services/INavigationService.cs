namespace FaizanCosmetics.UI.Services;

/// <summary>
/// Drives the MainWindow's content area. Views register a DataTemplate keyed by their ViewModel's
/// type (see MainWindow.xaml's resources), so navigating just sets CurrentView to a ViewModel
/// instance — WPF's implicit DataTemplate lookup renders the matching View.
/// </summary>
public interface INavigationService
{
    event Action<object>? CurrentViewChanged;
    object? CurrentView { get; }

    /// <summary>Resolves TViewModel through DI (use for real, fully-built module screens).</summary>
    void NavigateTo<TViewModel>() where TViewModel : class;

    /// <summary>Navigates directly to an already-constructed ViewModel instance (use when the
    /// screen needs per-navigation configuration, e.g. the PlaceholderViewModel's message).</summary>
    void NavigateToInstance(object viewModel);
}
