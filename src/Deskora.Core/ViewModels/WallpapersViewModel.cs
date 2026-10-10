namespace Deskora.Core.ViewModels;

public class WallpapersViewModel : ViewModelBase
{
    public string MilestoneNotice => "Milestone 0.2.0 — Local Wallpapers";
    public string Description => "This module will allow you to scan local folders, preview wallpapers with aspect ratio fitting, assign distinct images to multiple monitors, and keep a history of applied wallpapers.";

    public WallpapersViewModel()
    {
        Title = "Wallpapers";
    }
}
