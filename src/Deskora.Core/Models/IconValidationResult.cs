namespace Deskora.Core.Models;

/// <summary>
/// Detailed result of validating a prospective icon file (.ico, .exe, .dll).
/// </summary>
public sealed record IconValidationResult(
    bool IsValid,
    string? ErrorMessage = null,
    int BestWidth = 0,
    int BestHeight = 0,
    int ImageCount = 0,
    string FormatDescription = ""
);
