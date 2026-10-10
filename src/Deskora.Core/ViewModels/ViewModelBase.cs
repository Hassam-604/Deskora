using CommunityToolkit.Mvvm.ComponentModel;

namespace Deskora.Core.ViewModels;

/// <summary>
/// Common base class for all Deskora ViewModels.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;
}
