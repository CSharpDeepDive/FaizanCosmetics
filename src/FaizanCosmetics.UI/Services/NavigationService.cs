using Microsoft.Extensions.DependencyInjection;

namespace FaizanCosmetics.UI.Services;

/// <summary>
/// Creates a fresh DI scope for every navigated-to screen and disposes the previous one, so each
/// screen visit gets its own ApplicationDbContext instance instead of sharing one long-lived
/// context across the entire app session. Without this, "Scoped" services (including EF Core's
/// DbContext) resolved from this singleton's root IServiceProvider behave as if they were
/// singletons — which can surface as stale-looking data, unexpected change-tracker state, or
/// "a second operation was started on this context" exceptions under concurrent screens.
/// Dialogs opened from within a screen's ViewModel (via that ViewModel's own injected
/// IServiceProvider) automatically resolve from this same per-screen scope — that's a built-in
/// behavior of Microsoft.Extensions.DependencyInjection, not something this class has to arrange.
/// </summary>
public class NavigationService : INavigationService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IServiceScope? _currentScope;

    public NavigationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public object? CurrentView { get; private set; }
    public event Action<object>? CurrentViewChanged;

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var previousScope = _currentScope;
        var newScope = _scopeFactory.CreateScope();
        var viewModel = newScope.ServiceProvider.GetRequiredService<TViewModel>();

        _currentScope = newScope;
        CurrentView = viewModel;
        CurrentViewChanged?.Invoke(viewModel);

        // Dispose the old scope only after the new view is already live, so nothing mid-transition
        // is left referencing a disposed DbContext.
        previousScope?.Dispose();
    }

    public void NavigateToInstance(object viewModel)
    {
        // Used for ViewModels that don't need a database scope at all (e.g. PlaceholderViewModel,
        // constructed directly with `new`, no DI dependencies). Still retires whatever scope the
        // previous screen was using, since we're navigating away from it.
        var previousScope = _currentScope;
        _currentScope = null;

        CurrentView = viewModel;
        CurrentViewChanged?.Invoke(viewModel);

        previousScope?.Dispose();
    }

    public void Dispose()
    {
        _currentScope?.Dispose();
        GC.SuppressFinalize(this);
    }
}
