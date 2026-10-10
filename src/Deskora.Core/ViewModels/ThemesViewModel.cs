namespace Deskora.Core.ViewModels;

public class ThemesViewModel : ViewModelBase
{
    public string MilestoneNotice => "Milestone 0.8.0 — Themes & Integration";
    public string Description => "This module will unify desktop aesthetics by synchronizing Windows system accent colors, wallpapers, and matching widgets into shareable and recoverable theme bundles.";

    public ThemesViewModel()
    {
        Title = "Themes";
    }
}
