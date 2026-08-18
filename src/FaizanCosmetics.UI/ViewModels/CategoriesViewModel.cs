using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.UI.ViewModels;

public partial class CategoriesViewModel : ViewModelBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesViewModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
        _ = LoadAsync();
    }

    public ObservableCollection<CategoryDto> Categories { get; } = new();

    [ObservableProperty] private CategoryDto? selectedCategory;
    [ObservableProperty] private string newCategoryName = string.Empty;
    [ObservableProperty] private string? newCategoryDescription;
    [ObservableProperty] private string editName = string.Empty;
    [ObservableProperty] private string? editDescription;

    partial void OnSelectedCategoryChanged(CategoryDto? value)
    {
        EditName = value?.Name ?? string.Empty;
        EditDescription = value?.Description;
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var categories = await _categoryService.GetAllAsync(activeOnly: false);
            Categories.Clear();
            foreach (var category in categories) Categories.Add(category);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        ErrorMessage = null;
        try
        {
            await _categoryService.CreateAsync(NewCategoryName, NewCategoryDescription);
            NewCategoryName = string.Empty;
            NewCategoryDescription = null;
            await LoadAsync();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (SelectedCategory is null) return;
        ErrorMessage = null;
        try
        {
            await _categoryService.UpdateAsync(SelectedCategory.Id, EditName, EditDescription);
            await LoadAsync();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        if (SelectedCategory is null) return;
        ErrorMessage = null;
        try
        {
            await _categoryService.DeactivateAsync(SelectedCategory.Id);
            await LoadAsync();
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
