using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FaizanCosmetics.UI.ViewModels;

public partial class ClientsViewModel : ViewModelBase
{
    private const int PageSize = 50;

    private readonly IClientService _clientService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ClientsViewModel> _logger;

    public ClientsViewModel(IClientService clientService, IServiceProvider serviceProvider, ICurrentUserService currentUser, ILogger<ClientsViewModel> logger)
    {
        _clientService = clientService;
        _serviceProvider = serviceProvider;
        _currentUser = currentUser;
        _logger = logger;
        _ = LoadAsync();
    }

    public ObservableCollection<ClientListItemDto> Clients { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private ClientListItemDto? selectedClient;
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
    private void AddClient()
    {
        var dialog = _serviceProvider.GetRequiredService<Views.ClientEditWindow>();
        dialog.Initialize(clientId: null);
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    private void EditClient()
    {
        if (SelectedClient is null) return;
        var dialog = _serviceProvider.GetRequiredService<Views.ClientEditWindow>();
        dialog.Initialize(clientId: SelectedClient.Id);
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedClient is null) return;
        ErrorMessage = null;
        var currentUserId = _currentUser.UserId ?? 0;
        try
        {
            if (SelectedClient.IsActive) await _clientService.DeactivateAsync(SelectedClient.Id, currentUserId);
            else await _clientService.ReactivateAsync(SelectedClient.Id, currentUserId);
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
        if (SelectedClient is null) return;
        var dialog = _serviceProvider.GetRequiredService<Views.KhataStatementWindow>();
        dialog.Initialize(SelectedClient.Id);
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void ReceivePayment()
    {
        if (SelectedClient is null) return;
        var dialog = _serviceProvider.GetRequiredService<Views.ReceivePaymentWindow>();
        dialog.Initialize(SelectedClient.Id);
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var (items, total) = await _clientService.SearchAsync(SearchText, CurrentPage, PageSize);
            _logger.LogInformation("Client search returned {Count} of {Total} clients (page {Page}, search {SearchText}).", items.Count, total, CurrentPage, SearchText ?? "(none)");

            Clients.Clear();
            foreach (var item in items) Clients.Add(item);
            TotalCount = total;
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

            if (total == 0)
            {
                // Same temporary diagnostic intent as the catch block below — tells us definitively
                // "the query ran with no error and the database genuinely has zero matching rows"
                // rather than leaving a blank screen that looks identical to a swallowed exception.
                ErrorMessage = "Diagnostic: the client search completed with no error and returned 0 results.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load clients list (search {SearchText}, page {Page}).", SearchText ?? "(none)", CurrentPage);
            // Temporarily includes the technical exception message (normally we never do this —
            // see the project's error-handling policy) specifically so we can pin down the
            // "clients added but list stays empty" report with certainty instead of guessing
            // further. Revert to a plain friendly message once that's confirmed fixed.
            ErrorMessage = $"Unable to load clients. Technical detail: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
