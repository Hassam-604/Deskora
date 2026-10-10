using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Deskora.Core.Models;
using Deskora.Core.Services;

namespace Deskora.Core.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public ObservableCollection<FeatureInfo> PlannedFeatures { get; } = new();

    public HomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "Home";

        LoadPlannedFeatures();
    }

    private void LoadPlannedFeatures()
    {
        PlannedFeatures.Add(new FeatureInfo(
            "Local Wallpapers",
            "Browse, manage, and set high-resolution wallpapers from local folders across multiple displays.",
            "0.2.0",
            FeatureStatus.Planned,
            NavigationPageKey.Wallpapers,
            "IconWallpaper"
        ));

        PlannedFeatures.Add(new FeatureInfo(
            "Live & Video Wallpapers",
            "Hardware-accelerated live video and dynamic interactive wallpapers with low resource consumption.",
            "0.6.0",
            FeatureStatus.Planned,
            NavigationPageKey.LiveWallpapers,
            "IconVideo"
        ));

        PlannedFeatures.Add(new FeatureInfo(
            "Desktop Icon Customization",
            "Personalize desktop shortcut styles, package icons, and manage system icon appearance.",
            "0.4.0",
            FeatureStatus.Planned,
            NavigationPageKey.Icons,
            "IconGrid"
        ));

        PlannedFeatures.Add(new FeatureInfo(
            "Desktop Layouts & Presets",
            "Save and restore exact desktop icon positions when changing monitor resolutions or docking.",
            "0.5.0",
            FeatureStatus.Planned,
            NavigationPageKey.Layouts,
            "IconLayout"
        ));

        PlannedFeatures.Add(new FeatureInfo(
            "Curated Themes",
            "Synchronize Windows accent colors, UI chrome, and desktop aesthetics with tailored presets.",
            "0.8.0",
            FeatureStatus.Planned,
            NavigationPageKey.Themes,
            "IconPalette"
        ));

        PlannedFeatures.Add(new FeatureInfo(
            "Desktop Widgets",
            "Lightweight floating and in-app system statistics, clocks, and customization widgets.",
            "0.7.0",
            FeatureStatus.Planned,
            NavigationPageKey.Widgets,
            "IconWidget"
        ));
    }

    [RelayCommand]
    private void NavigateToFeature(NavigationPageKey key)
    {
        _navigationService.NavigateTo(key);
    }
}
