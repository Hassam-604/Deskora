using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Deskora.Core.ViewModels;

namespace Deskora.App;

/// <summary>
/// Maps ViewModels to their corresponding Avalonia Views dynamically.
/// Example: Deskora.Core.ViewModels.HomeViewModel -> Deskora.App.Views.HomeView
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        var name = param.GetType().FullName!
            .Replace("Deskora.Core.ViewModels", "Deskora.App.Views")
            .Replace("ViewModel", "View");

        var type = Type.GetType(name) ?? typeof(ViewLocator).Assembly.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = $"View Not Found for: {name}" };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
