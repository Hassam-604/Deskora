namespace Deskora.Core.ViewModels;

public class WidgetsViewModel : ViewModelBase
{
    public string MilestoneNotice => "Milestone 0.7.0 — In-App & Floating Widgets";
    public string Description => "This module will deliver ultra-lightweight desktop widgets (CPU/RAM meters, system clocks, quick scratchpads) designed with strict memory budgets and zero unnecessary background polling.";

    public WidgetsViewModel()
    {
        Title = "Widgets";
    }
}
