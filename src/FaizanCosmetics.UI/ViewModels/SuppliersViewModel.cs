using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FaizanCosmetics.UI.ViewModels;

public partial class SuppliersViewModel : ViewModelBase
{
    private const int PageSize = 50;

    private readonly ISupplierService _supplierService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SuppliersViewModel> _logger;

    public SuppliersViewModel(ISupplierService supplierService, IServiceProvider serviceProvider, ICurrentUserService currentUser, ILogger<SuppliersViewModel> logger)
    {
        _supplierService = supplierService;
        _serviceProvider = serviceProvider;
        _currentUser = currentUser;
        _logger = logger;
        _ = LoadAsync();
    }

    public ObservableCollection<SupplierListItemDto> Suppliers { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private SupplierListItemDto? selectedSupplier;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private int totalCount;

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages) { CurrentPage++; await LoadAsync(); }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1) { CurrentPage--; await LoadAsync(); }
    }

    [RelayCommand]
    private void AddSupplier()
    {
        var dialog = _serviceProvider.GetRequiredService<Views.SupplierEditWindow>();
        dialog.Initialize(supplierId: null);
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    private void EditSupplier()
    {
        if (SelectedSupplier is null) return;
        var dialog = _serviceProvider.GetRequiredService<Views.SupplierEditWindow>();
        dialog.Initialize(supplierId: SelectedSupplier.Id);
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedSupplier is null) return;
        ErrorMessage = null;
        var currentUserId = _currentUser.UserId ?? 0;
        try
        {
            if (SelectedSupplier.IsActive) await _supplierService.DeactivateAsync(SelectedSupplier.Id, currentUserId);
            else await _supplierService.ReactivateAsync(SelectedSupplier.Id, currentUserId);
            await LoadAsync();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void ViewStatement()
    {
        if (SelectedSupplier is null) return;
        var dialog = _serviceProvider.GetRequiredService<Views.SupplierStatementWindow>();
        dialog.Initialize(SelectedSupplier.Id);
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void PaySupplier()
    {
        if (SelectedSupplier is null) return;
        var dialog = _serviceProvider.GetRequiredService<Views.SupplierPaymentWindow>();
        dialog.Initialize(SelectedSupplier.Id);
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (items, total) = await _supplierService.SearchAsync(SearchText, CurrentPage, PageSize);
            _logger.LogInformation("Supplier search returned {Count} of {Total} suppliers.", items.Count, total);

            Suppliers.Clear();
            foreach (var item in items) Suppliers.Add(item);
            TotalCount = total;
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load suppliers list.");
            ErrorMessage = "Unable to load suppliers. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
