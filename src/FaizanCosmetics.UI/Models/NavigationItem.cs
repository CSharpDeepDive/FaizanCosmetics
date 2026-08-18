using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Domain.Enums;
using FaizanCosmetics.UI.Services;

namespace FaizanCosmetics.UI.Models;

/// <summary>
/// One clickable entry in the sidebar. ViewModelFactory produces the ViewModel to navigate to —
/// for a fully-built module this is typically "() => navigationService.NavigateTo&lt;XViewModel&gt;()"
/// resolved via DI inside the factory delegate; for an unbuilt module it constructs a configured
/// PlaceholderViewModel instead. RequiredRoles empty means visible to every role.
/// </summary>
public partial class NavigationItem : ObservableObject
{
    private readonly Action _navigate;

    public NavigationItem(string title, Action navigate, params UserRole[] requiredRoles)
    {
        Title = title;
        _navigate = navigate;
        RequiredRoles = requiredRoles;
    }

    public string Title { get; }
    public UserRole[] RequiredRoles { get; }

    [ObservableProperty]
    private bool isSelected;

    [RelayCommand]
    private void Navigate() => _navigate();
}

public class NavigationSection
{
    public string Title { get; init; } = string.Empty;
    public List<NavigationItem> Items { get; init; } = new();
}
