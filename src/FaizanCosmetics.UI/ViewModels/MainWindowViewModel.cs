using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.UI.Models;
using FaizanCosmetics.UI.Services;
using FaizanCosmetics.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FaizanCosmetics.UI.ViewModels;

/// <summary>
/// Root shell ViewModel: builds the role-filtered navigation menu and hosts the currently
/// navigated screen (via INavigationService.CurrentView, surfaced through the ContentControl in
/// MainWindow.xaml). Real modules use NavigateTo&lt;TViewModel&gt;() (DI-resolved); modules not yet
/// built in this phased delivery use a configured PlaceholderViewModel instead of a dead button.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserRole _currentRole;

    public MainWindowViewModel(ICurrentUserService currentUser, INavigationService navigationService, IServiceScopeFactory scopeFactory)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        _currentRole = currentUser.Role ?? UserRole.Cashier;

        CurrentUserFullName = currentUser.FullName ?? string.Empty;
        CurrentUserRole = currentUser.Role?.ToString() ?? string.Empty;

        _navigationService.CurrentViewChanged += viewModel => CurrentView = viewModel;

        NavigationSections = BuildNavigationSections();
        _navigationService.NavigateTo<DashboardViewModel>();
    }

    [ObservableProperty]
    private string currentUserFullName;

    [ObservableProperty]
    private string currentUserRole;

    [ObservableProperty]
    private DateTime currentDateTime = DateTime.Now;

    [ObservableProperty]
    private object? currentView;

    public ObservableCollection<NavigationSection> NavigationSections { get; }

    private ObservableCollection<NavigationSection> BuildNavigationSections()
    {
        var sections = new List<NavigationSection>
        {
            new()
            {
                Title = "",
                Items = { new NavigationItem("Dashboard", () => _navigationService.NavigateTo<DashboardViewModel>()) }
            },
            new()
            {
                Title = "SALES",
                Items =
                {
                    new NavigationItem("New Invoice", () => _navigationService.NavigateTo<SalesInvoiceViewModel>()),
                    new NavigationItem("Sales History", () => _navigationService.NavigateTo<SalesHistoryViewModel>()),
                    Placeholder("Sales Return", "Phase 7 — Returns & Inventory Adjustments"),
                    new NavigationItem("Receive Payment", () => _navigationService.NavigateTo<ClientsViewModel>()),
                }
            },
            new()
            {
                Title = "PRODUCTS",
                Items =
                {
                    new NavigationItem("Products", () => _navigationService.NavigateTo<ProductsViewModel>()),
                    new NavigationItem("Categories", OpenCategoriesDialog),
                    new NavigationItem("Low Stock", () => _navigationService.NavigateTo<LowStockViewModel>()),
                    Placeholder("Stock Adjustment", "Phase 7 — Returns & Inventory Adjustments"),
                }
            },
            new()
            {
                Title = "CLIENTS",
                Items =
                {
                    new NavigationItem("Clients", () => _navigationService.NavigateTo<ClientsViewModel>()),
                    new NavigationItem("Khata", () => _navigationService.NavigateTo<ClientsViewModel>()),
                    new NavigationItem("Receive Payment", () => _navigationService.NavigateTo<ClientsViewModel>()),
                }
            },
            new()
            {
                Title = "PURCHASES",
                Items =
                {
                    new NavigationItem("Suppliers", () => _navigationService.NavigateTo<SuppliersViewModel>()),
                    Placeholder("Purchase Orders", "A later phase — direct Purchase Invoice entry is used for now; see Purchases"),
                    new NavigationItem("Purchases", () => _navigationService.NavigateTo<PurchaseInvoiceViewModel>()),
                    Placeholder("Purchase Returns", "Phase 7 — Returns & Inventory Adjustments"),
                    new NavigationItem("Purchase History", () => _navigationService.NavigateTo<PurchaseHistoryViewModel>()),
                    new NavigationItem("Supplier Payments", () => _navigationService.NavigateTo<SuppliersViewModel>()),
                }
            },
            new()
            {
                Title = "",
                Items = { Placeholder("Reports", "Phase 8 — Reports & Analytics") }
            }
        };

        // Administration is only offered to Admin/Manager — a Cashier never even sees the entries,
        // not merely a disabled button, since a hidden action can't be misclicked or probed.
        if (_currentRole is UserRole.Admin or UserRole.Manager)
        {
            sections.Add(new NavigationSection
            {
                Title = "ADMINISTRATION",
                Items =
                {
                    Placeholder("Users", "Phase 10 — Audit Logging, Error Handling & Final Polishing", UserRole.Admin),
                    Placeholder("Audit Logs", "Phase 10 — Audit Logging, Error Handling & Final Polishing", UserRole.Admin, UserRole.Manager),
                    Placeholder("Settings", "Phase 9 — Printing, Excel/PDF Export, Backup/Restore", UserRole.Admin),
                    Placeholder("Backup/Restore", "Phase 9 — Printing, Excel/PDF Export, Backup/Restore", UserRole.Admin),
                }
            });
        }

        // Drop any item whose RequiredRoles don't include the current role (empty = everyone).
        foreach (var section in sections)
        {
            section.Items.RemoveAll(item => item.RequiredRoles.Length > 0 && !item.RequiredRoles.Contains(_currentRole));
        }

        return new ObservableCollection<NavigationSection>(sections.Where(s => s.Items.Count > 0));
    }

    private NavigationItem Placeholder(string title, string plannedPhase, params UserRole[] requiredRoles) =>
        new(title, () => _navigationService.NavigateToInstance(new PlaceholderViewModel { ModuleName = title, PlannedPhase = $"Coming in {plannedPhase}." }), requiredRoles);

    /// <summary>The Categories window is a modal dialog, not a navigated screen, so it gets its
    /// own short-lived scope (created and disposed around the single ShowDialog() call) rather
    /// than sharing whatever scope the currently-navigated screen happens to be using.</summary>
    private void OpenCategoriesDialog()
    {
        using var scope = _scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<CategoriesWindow>().ShowDialog();
    }
}
