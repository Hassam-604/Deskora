namespace Deskora.Core.ViewModels;

public class IconsViewModel : ViewModelBase
{
    public string MilestoneNotice => "Milestone 0.4.0 — Desktop Icon Customization";
    public string Description => "This module will provide tools to customize desktop shortcut icons, swap system folder glyphs, and restore default Windows shell icons safely with built-in backup points.";

    public IconsViewModel()
    {
        Title = "Icons";
    }
}
