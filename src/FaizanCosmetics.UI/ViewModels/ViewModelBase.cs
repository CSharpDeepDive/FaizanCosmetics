using CommunityToolkit.Mvvm.ComponentModel;

namespace FaizanCosmetics.UI.ViewModels;

/// <summary>Common base for all ViewModels — adds a busy flag and error message slot used consistently across screens.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;
}
