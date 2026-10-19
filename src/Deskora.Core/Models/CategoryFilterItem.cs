using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskora.Core.Models;

/// <summary>
/// Represents a selectable category filter chip in the icon library.
/// </summary>
public sealed partial class CategoryFilterItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    public CategoryFilterItem(string name, bool isSelected = false)
    {
        Name = name;
        IsSelected = isSelected;
    }
}
