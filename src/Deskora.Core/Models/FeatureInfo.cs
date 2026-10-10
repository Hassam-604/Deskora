namespace Deskora.Core.Models;

/// <summary>
/// Development lifecycle state for a feature.
/// </summary>
public enum FeatureStatus
{
    Planned,
    InDevelopment,
    Active,
    Experimental
}

/// <summary>
/// Metadata describing a feature module on the dashboard.
/// </summary>
public record FeatureInfo(
    string Title,
    string Description,
    string MilestoneTag,
    FeatureStatus Status,
    NavigationPageKey PageKey,
    string IconKey
);
