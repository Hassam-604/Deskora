namespace Deskora.Core.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public string AppName => "Deskora";
    public string Tagline => "Desktop Personalization Studio";
    public string Version => "Version 0.1.0-alpha";
    public string Platform => "Windows (Cross-platform foundation via Avalonia UI)";
    public string TechStack => "C#, .NET 8.0 LTS, Avalonia UI 11.2.5, CommunityToolkit.Mvvm";
    public string LicenseNotice => "License to be determined by Product Owner. All rights reserved.";

    public string Description =>
        "Deskora is a desktop personalization toolkit built incrementally with an uncompromising focus on " +
        "performance, reliability, and user safety. It provides a cohesive studio to manage wallpapers, icons, " +
        "layouts, themes, and lightweight desktop widgets.";

    public AboutViewModel()
    {
        Title = "About";
    }
}
