using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskora.Core.Models;

/// <summary>
/// Represents a navigation destination item in the application sidebar.
/// </summary>
public partial class NavigationItem : ObservableObject
{
    public NavigationPageKey Key { get; }
    public string Label { get; }
    public string IconKey { get; }
    public string? BadgeText { get; }

    [ObservableProperty]
    private bool _isSelected;

    public NavigationItem(NavigationPageKey key, string label, string iconKey, string? badgeText = null)
    {
        Key = key;
        Label = label;
        IconKey = iconKey;
        BadgeText = badgeText;
    }
}
