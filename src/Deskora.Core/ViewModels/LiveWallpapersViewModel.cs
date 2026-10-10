namespace Deskora.Core.ViewModels;

public class LiveWallpapersViewModel : ViewModelBase
{
    public string MilestoneNotice => "Milestone 0.6.0 — Live Wallpaper Proof of Concept";
    public string Description => "This experimental module will host video-based and interactive desktop backgrounds using low-overhead Windows WorkerW canvas hooking, with automatic suspension when games or full-screen apps are running.";

    public LiveWallpapersViewModel()
    {
        Title = "Live Wallpapers";
    }
}
