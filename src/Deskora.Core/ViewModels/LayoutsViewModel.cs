namespace Deskora.Core.ViewModels;

public class LayoutsViewModel : ViewModelBase
{
    public string MilestoneNotice => "Milestone 0.5.0 — Desktop Layouts & Presets";
    public string Description => "This module will capture and restore exact desktop icon coordinates across display resolution changes, monitor disconnections, and docking stations, with named profile presets.";

    public LayoutsViewModel()
    {
        Title = "Layouts";
    }
}
