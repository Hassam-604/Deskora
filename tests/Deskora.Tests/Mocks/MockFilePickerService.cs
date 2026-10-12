using Deskora.Core.Services;

namespace Deskora.Tests.Mocks;

public class MockFilePickerService : IFilePickerService
{
    public List<string> FilesToReturn { get; } = new();
    public string? FolderToReturn { get; set; }

    public int PickFilesCallCount { get; private set; }
    public int PickFolderCallCount { get; private set; }

    public Task<IReadOnlyList<string>> PickImageFilesAsync()
    {
        PickFilesCallCount++;
        return Task.FromResult<IReadOnlyList<string>>(FilesToReturn.ToList());
    }

    public Task<string?> PickFolderAsync()
    {
        PickFolderCallCount++;
        return Task.FromResult(FolderToReturn);
    }
}
