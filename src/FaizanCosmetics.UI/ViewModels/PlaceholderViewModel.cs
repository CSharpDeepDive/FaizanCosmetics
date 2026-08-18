using CommunityToolkit.Mvvm.ComponentModel;

namespace FaizanCosmetics.UI.ViewModels;

/// <summary>
/// Shown for navigation items whose real module hasn't been built yet in the phased delivery
/// plan. This is an honest "not built yet" screen, not a fake/non-functional feature — it tells
/// the user exactly that, rather than pretending a button does something it doesn't.
/// </summary>
public partial class PlaceholderViewModel : ViewModelBase
{
    [ObservableProperty]
    private string moduleName = string.Empty;

    [ObservableProperty]
    private string plannedPhase = string.Empty;
}
