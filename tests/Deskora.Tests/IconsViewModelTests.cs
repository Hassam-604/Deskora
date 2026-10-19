using Deskora.Core.Models;
using Deskora.Core.Services;
using Deskora.Core.ViewModels;
using Deskora.Tests.Mocks;

namespace Deskora.Tests;

public class IconsViewModelTests : IDisposable
{
    private readonly string _tempDir;

    public IconsViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"Deskora_IconsVmTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private string CreateSampleIco(string filename)
    {
        var path = Path.Combine(_tempDir, filename);
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write((ushort)0); // reserved
            writer.Write((ushort)1); // type 1
            writer.Write((ushort)1); // count 1
            writer.Write((byte)32);  // width
            writer.Write((byte)32);  // height
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)4);
            writer.Write((uint)22);
            writer.Write(new byte[4]);
        }
        return path;
    }

    [Fact]
    public async Task ScanDesktopAsync_PopulatesIconsAndUpdatesCounts()
    {
        var mockService = new MockIconService();
        mockService.IconsToReturn.Add(new DesktopIconItem
        {
            Id = "1",
            Name = "Terminal",
            FilePath = "C:\\Desktop\\Terminal.lnk",
            TargetPath = "C:\\Windows\\terminal.exe"
        });
        mockService.IconsToReturn.Add(new DesktopIconItem
        {
            Id = "2",
            Name = "Projects",
            FilePath = "C:\\Desktop\\Projects",
            TargetPath = "C:\\Desktop\\Projects",
            IsFolder = true
        });

        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors);

        await vm.ScanDesktopAsync();

        Assert.Equal(2, vm.DesktopIcons.Count);
        Assert.Equal(2, vm.DisplayedIcons.Count);
        Assert.Equal(2, vm.TotalCount);
        Assert.Equal(0, vm.ModifiedCount);
        Assert.NotNull(vm.SelectedIcon);
        Assert.Equal("Terminal", vm.SelectedIcon.Name);
    }

    [Fact]
    public async Task SearchQuery_FiltersDisplayedIcons()
    {
        var mockService = new MockIconService();
        mockService.IconsToReturn.Add(new DesktopIconItem { Id = "1", Name = "Terminal", FilePath = "C:\\Terminal.lnk" });
        mockService.IconsToReturn.Add(new DesktopIconItem { Id = "2", Name = "Firefox", FilePath = "C:\\Firefox.lnk" });

        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors);
        await vm.ScanDesktopAsync();

        vm.SearchQuery = "Fire";

        Assert.Single(vm.DisplayedIcons);
        Assert.Equal("Firefox", vm.DisplayedIcons[0].Name);

        vm.ClearSearch();

        Assert.Equal(2, vm.DisplayedIcons.Count);
    }

    [Fact]
    public async Task ApplyCustomIconAsync_WhenValidIconSelected_AppliesSuccessfully()
    {
        var mockService = new MockIconService();
        var item = new DesktopIconItem
        {
            Id = "1",
            Name = "App",
            FilePath = "C:\\Desktop\\App.lnk",
            OriginalIconPath = "C:\\original.ico",
            OriginalIconIndex = 0
        };
        mockService.IconsToReturn.Add(item);

        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors);
        await vm.ScanDesktopAsync();

        var validIco = CreateSampleIco("custom.ico");
        vm.SelectedCustomIconPath = validIco;

        Assert.True(vm.IsCustomIconValid);

        await vm.ApplyCustomIconAsync();

        Assert.Equal(1, mockService.ApplyCallCount);
        Assert.True(item.IsModified);
        Assert.Equal(validIco, item.CurrentIconPath);
        Assert.Equal(1, vm.ModifiedCount);
    }

    [Fact]
    public async Task RestoreSelectedAsync_RevertsIconToOriginal()
    {
        var mockService = new MockIconService();
        var item = new DesktopIconItem
        {
            Id = "1",
            Name = "App",
            FilePath = "C:\\Desktop\\App.lnk",
            CurrentIconPath = "C:\\custom.ico",
            OriginalIconPath = "C:\\original.ico",
            OriginalIconIndex = 0,
            IsModified = true
        };
        mockService.IconsToReturn.Add(item);

        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors);
        await vm.ScanDesktopAsync();

        Assert.Equal(1, vm.ModifiedCount);

        await vm.RestoreSelectedAsync();

        Assert.Equal(1, mockService.RestoreCallCount);
        Assert.False(item.IsModified);
        Assert.Equal("C:\\original.ico", item.CurrentIconPath);
        Assert.Equal(0, vm.ModifiedCount);
    }

    [Fact]
    public async Task ResetAllToDefaultsAsync_RevertsAllCustomIcons()
    {
        var mockService = new MockIconService();
        mockService.IconsToReturn.Add(new DesktopIconItem
        {
            Id = "1",
            Name = "App 1",
            FilePath = "C:\\App1.lnk",
            CurrentIconPath = "C:\\custom1.ico",
            OriginalIconPath = "C:\\orig1.ico",
            IsModified = true
        });
        mockService.IconsToReturn.Add(new DesktopIconItem
        {
            Id = "2",
            Name = "App 2",
            FilePath = "C:\\App2.lnk",
            CurrentIconPath = "C:\\custom2.ico",
            OriginalIconPath = "C:\\orig2.ico",
            IsModified = true
        });

        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors);
        await vm.ScanDesktopAsync();

        Assert.Equal(2, vm.ModifiedCount);

        await vm.ResetAllToDefaultsAsync();

        Assert.Equal(1, mockService.ResetAllCallCount);
        Assert.Equal(0, vm.ModifiedCount);
        Assert.All(vm.DesktopIcons, i => Assert.False(i.IsModified));
    }

    [Fact]
    public async Task RefreshShellCacheAsync_InvokesServiceNotification()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors);

        await vm.RefreshShellCacheAsync();

        Assert.Equal(1, mockService.RefreshShellCallCount);
    }

    [Fact]
    public void SelectionChange_ResetsSelectedCustomIconPathAndPreview()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors);
        var item1 = new DesktopIconItem { Id = "1", Name = "Item 1", FilePath = "C:\\Item1.lnk" };
        var item2 = new DesktopIconItem { Id = "2", Name = "Item 2", FilePath = "C:\\Item2.lnk" };

        vm.SelectIcon(item1);
        var validIco = CreateSampleIco("test_preview.ico");
        vm.SelectedCustomIconPath = validIco;

        Assert.Equal(validIco, vm.SelectedCustomIconPath);

        // Switch selection
        vm.SelectIcon(item2);

        Assert.Null(vm.SelectedCustomIconPath);
        Assert.Null(vm.CustomIconPreviewBitmap);
    }
    [Fact]
    public async Task OpenLibraryAsync_LoadsIconsAndPopulatesCollections()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        mockLib.SystemIconsToReturn.Add(new SystemIconItem
        {
            Id = "imageres_3",
            Title = "imageres.dll #3",
            LibraryPath = "C:\\Windows\\System32\\imageres.dll",
            IconIndex = 3,
            Category = "Folders"
        });
        mockLib.CuratedIconsToReturn.Add(new WebIconItem
        {
            Id = "app_chrome",
            Name = "Google Chrome",
            Category = "Apps"
        });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        Assert.True(vm.IsLibraryOpen);
        Assert.Single(vm.SystemIcons);
        Assert.Single(vm.CuratedIcons);
        Assert.Single(vm.DisplayedSystemIcons);
        Assert.Single(vm.DisplayedCuratedIcons);
    }

    [Fact]
    public async Task SelectCategory_FiltersDisplayedIcons()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        mockLib.SystemIconsToReturn.Add(new SystemIconItem
        {
            Id = "sys_folder",
            Title = "Folder",
            LibraryPath = "C:\\imageres.dll",
            IconIndex = 3,
            Category = "Folders"
        });
        mockLib.SystemIconsToReturn.Add(new SystemIconItem
        {
            Id = "sys_device",
            Title = "Device",
            LibraryPath = "C:\\imageres.dll",
            IconIndex = 25,
            Category = "Devices"
        });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        Assert.Equal(2, vm.DisplayedSystemIcons.Count);

        vm.SelectCategory("Folders");
        Assert.Single(vm.DisplayedSystemIcons);
        Assert.Equal("Folders", vm.DisplayedSystemIcons[0].Category);
    }

    [Fact]
    public void SelectSystemIcon_SetsCustomIconAndValidationMessage()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        var sysItem = new SystemIconItem
        {
            Id = "imageres_3",
            Title = "Folder 3",
            LibraryPath = "C:\\Windows\\System32\\imageres.dll",
            IconIndex = 3,
            Category = "Folders"
        };

        vm.SelectSystemIcon(sysItem);

        Assert.Equal("C:\\Windows\\System32\\imageres.dll", vm.SelectedCustomIconPath);
        Assert.Equal(3, vm.SelectedCustomIconIndex);
        Assert.True(vm.IsCustomIconValid);
        Assert.False(vm.IsLibraryOpen);
        Assert.Contains("Windows Icon", vm.CustomIconValidationMessage);
    }

    [Fact]
    public async Task SelectWebIconAsync_PreparesAndSetsCustomIcon()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        var webItem = new WebIconItem
        {
            Id = "app_chrome",
            Name = "Google Chrome",
            Category = "Apps",
            LocalCachedPath = "C:\\Icons\\chrome.ico"
        };

        await vm.SelectWebIconAsync(webItem);

        Assert.Equal("C:\\Icons\\chrome.ico", vm.SelectedCustomIconPath);
        Assert.Equal(0, vm.SelectedCustomIconIndex);
        Assert.True(vm.IsCustomIconValid);
        Assert.False(vm.IsLibraryOpen);
        Assert.Contains("Google Chrome", vm.CustomIconValidationMessage);
    }

    [Fact]
    public async Task SelectLibraryTab_TogglesActiveTabAndUpdatesCount()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        mockLib.SystemIconsToReturn.Add(new SystemIconItem
        {
            Id = "sys_1",
            Title = "Icon 1",
            LibraryPath = "C:\\imageres.dll",
            IconIndex = 1,
            Category = "Folders"
        });
        mockLib.SystemIconsToReturn.Add(new SystemIconItem
        {
            Id = "sys_2",
            Title = "Icon 2",
            LibraryPath = "C:\\imageres.dll",
            IconIndex = 2,
            Category = "Devices"
        });
        mockLib.CuratedIconsToReturn.Add(new WebIconItem
        {
            Id = "app_chrome",
            Name = "Chrome",
            Category = "Apps"
        });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        // Default tab is System
        Assert.True(vm.IsSystemTabSelected);
        Assert.False(vm.IsCuratedTabSelected);
        Assert.Equal(2, vm.LibraryDisplayedCount);

        // Switch to Curated tab
        vm.SelectLibraryTab("Curated");
        Assert.False(vm.IsSystemTabSelected);
        Assert.True(vm.IsCuratedTabSelected);
        Assert.Equal(1, vm.LibraryDisplayedCount);

        // Switch back to System tab
        vm.SelectLibraryTab("System");
        Assert.True(vm.IsSystemTabSelected);
        Assert.False(vm.IsCuratedTabSelected);
        Assert.Equal(2, vm.LibraryDisplayedCount);
    }

    [Fact]
    public async Task Categories_DefaultHasAllSelected_AndSelectionUpdates()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        mockLib.SystemIconsToReturn.Add(new SystemIconItem
        {
            Id = "sys_f",
            Title = "Folder 1",
            LibraryPath = "C:\\imageres.dll",
            IconIndex = 3,
            Category = "Folders"
        });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        var allCat = vm.Categories.First(c => c.Name == "All");
        var foldersCat = vm.Categories.First(c => c.Name == "Folders");

        Assert.True(allCat.IsSelected);
        Assert.False(foldersCat.IsSelected);

        vm.SelectCategory("Folders");

        Assert.False(allCat.IsSelected);
        Assert.True(foldersCat.IsSelected);
        Assert.Equal("Folders", vm.SelectedCategory);
    }

    [Fact]
    public async Task WindowsIcons_DisplaysOnlyActualAvailableCategories_AndHidesEmptyOnes()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        // System source has only Folders and Devices
        mockLib.SystemIconsToReturn.Add(new SystemIconItem { Id = "s1", Title = "Folder", LibraryPath = "C:\\a.dll", IconIndex = 1, Category = "Folders" });
        mockLib.SystemIconsToReturn.Add(new SystemIconItem { Id = "s2", Title = "Device", LibraryPath = "C:\\a.dll", IconIndex = 2, Category = "Devices" });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        // Should contain All, Folders, Devices (3 items)
        Assert.Equal(3, vm.Categories.Count);
        Assert.Contains(vm.Categories, c => c.Name == "All");
        Assert.Contains(vm.Categories, c => c.Name == "Folders");
        Assert.Contains(vm.Categories, c => c.Name == "Devices");

        // Should NOT contain empty categories
        Assert.DoesNotContain(vm.Categories, c => c.Name == "System");
        Assert.DoesNotContain(vm.Categories, c => c.Name == "Media");
        Assert.DoesNotContain(vm.Categories, c => c.Name == "Apps");
    }

    [Fact]
    public async Task CustomIconLibrary_DisplaysOnlyActualAvailableCategories_AndHidesEmptyOnes()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        // Curated has Apps, Folders, Media (no Devices or System)
        mockLib.CuratedIconsToReturn.Add(new WebIconItem { Id = "c1", Name = "Chrome", Category = "Apps" });
        mockLib.CuratedIconsToReturn.Add(new WebIconItem { Id = "c2", Name = "Cyan Folder", Category = "Folders" });
        mockLib.CuratedIconsToReturn.Add(new WebIconItem { Id = "c3", Name = "YouTube", Category = "Media" });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        // Switch to Custom Icon Library (Curated)
        vm.SelectLibraryTab("Curated");

        // Should contain All, Folders, Media, Apps (4 items)
        Assert.Equal(4, vm.Categories.Count);
        Assert.Contains(vm.Categories, c => c.Name == "All");
        Assert.Contains(vm.Categories, c => c.Name == "Folders");
        Assert.Contains(vm.Categories, c => c.Name == "Media");
        Assert.Contains(vm.Categories, c => c.Name == "Apps");

        // Empty categories must NOT appear
        Assert.DoesNotContain(vm.Categories, c => c.Name == "Devices");
        Assert.DoesNotContain(vm.Categories, c => c.Name == "System");
    }

    [Fact]
    public async Task SwitchingSources_ResetsUnavailableCategoryToAll()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        // System has Devices
        mockLib.SystemIconsToReturn.Add(new SystemIconItem { Id = "s1", Title = "Drive", LibraryPath = "C:\\a.dll", IconIndex = 1, Category = "Devices" });
        // Curated only has Apps
        mockLib.CuratedIconsToReturn.Add(new WebIconItem { Id = "c1", Name = "Discord", Category = "Apps" });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        // Select Devices in Windows Icons
        vm.SelectCategory("Devices");
        Assert.Equal("Devices", vm.SelectedCategory);

        // Switch to Custom Icon Library (which does NOT have Devices)
        vm.SelectLibraryTab("Curated");

        // Category must reset to All
        Assert.Equal("All", vm.SelectedCategory);
        Assert.True(vm.Categories.First(c => c.Name == "All").IsSelected);
        Assert.DoesNotContain(vm.Categories, c => c.Name == "Devices");
    }

    [Fact]
    public async Task SwitchingSources_PreservesCategoryIfAvailableInBothSources()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        // Both sources have Folders
        mockLib.SystemIconsToReturn.Add(new SystemIconItem { Id = "s1", Title = "Folder", LibraryPath = "C:\\a.dll", IconIndex = 1, Category = "Folders" });
        mockLib.CuratedIconsToReturn.Add(new WebIconItem { Id = "c1", Name = "Cyan Folder", Category = "Folders" });
        mockLib.CuratedIconsToReturn.Add(new WebIconItem { Id = "c2", Name = "Chrome", Category = "Apps" });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        // Select Folders in Windows Icons
        vm.SelectCategory("Folders");
        Assert.Equal("Folders", vm.SelectedCategory);

        // Switch to Custom Icon Library (which ALSO has Folders)
        vm.SelectLibraryTab("Curated");

        // Category must be preserved
        Assert.Equal("Folders", vm.SelectedCategory);
        Assert.True(vm.Categories.First(c => c.Name == "Folders").IsSelected);
        Assert.False(vm.Categories.First(c => c.Name == "All").IsSelected);
        Assert.Single(vm.DisplayedCuratedIcons);
    }

    [Fact]
    public async Task EmptyCollection_ProducesClearEmptyStateAndEmptyCategories()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        // Both collections empty
        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        Assert.Empty(vm.Categories);
        Assert.Equal(0, vm.LibraryDisplayedCount);
        Assert.Empty(vm.DisplayedSystemIcons);

        // Switch to curated which is also empty
        vm.SelectLibraryTab("Curated");
        Assert.Empty(vm.Categories);
        Assert.Equal(0, vm.LibraryDisplayedCount);
        Assert.Empty(vm.DisplayedCuratedIcons);
    }

    [Fact]
    public async Task SwitchingBackToPreviousSource_RestoresItsCorrectResults()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        mockLib.SystemIconsToReturn.Add(new SystemIconItem { Id = "s1", Title = "System 1", LibraryPath = "C:\\a.dll", IconIndex = 1, Category = "System" });
        mockLib.SystemIconsToReturn.Add(new SystemIconItem { Id = "s2", Title = "System 2", LibraryPath = "C:\\a.dll", IconIndex = 2, Category = "System" });
        mockLib.CuratedIconsToReturn.Add(new WebIconItem { Id = "c1", Name = "App 1", Category = "Apps" });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        Assert.Equal(2, vm.LibraryDisplayedCount);
        Assert.Contains(vm.Categories, c => c.Name == "System");

        // Switch to Curated
        vm.SelectLibraryTab("Curated");
        Assert.Equal(1, vm.LibraryDisplayedCount);
        Assert.Contains(vm.Categories, c => c.Name == "Apps");
        Assert.DoesNotContain(vm.Categories, c => c.Name == "System");

        // Switch back to System
        vm.SelectLibraryTab("System");
        Assert.Equal(2, vm.LibraryDisplayedCount);
        Assert.Contains(vm.Categories, c => c.Name == "System");
        Assert.DoesNotContain(vm.Categories, c => c.Name == "Apps");
    }

    [Fact]
    public async Task ResetLibraryFilters_ClearsQueryAndResetsToAll()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        mockLib.SystemIconsToReturn.Add(new SystemIconItem { Id = "s1", Title = "Terminal", LibraryPath = "C:\\a.dll", IconIndex = 1, Category = "System" });
        mockLib.SystemIconsToReturn.Add(new SystemIconItem { Id = "s2", Title = "Folder", LibraryPath = "C:\\a.dll", IconIndex = 2, Category = "Folders" });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        vm.SelectCategory("System");
        vm.LibrarySearchQuery = "Terminal";

        Assert.Equal("System", vm.SelectedCategory);
        Assert.Equal("Terminal", vm.LibrarySearchQuery);

        vm.ResetLibraryFilters();

        Assert.Equal("All", vm.SelectedCategory);
        Assert.Equal(string.Empty, vm.LibrarySearchQuery);
        Assert.True(vm.Categories.First(c => c.Name == "All").IsSelected);
        Assert.False(vm.Categories.First(c => c.Name == "System").IsSelected);
    }

    [Fact]
    public async Task SearchAndCategory_CombineCorrectly_AndHandleEmptyResults()
    {
        var mockService = new MockIconService();
        var mockPrefs = new MockIconPreferencesService();
        var mockPicker = new MockFilePickerService();
        var mockErrors = new ErrorService();
        var mockLib = new MockIconLibraryService();

        mockLib.SystemIconsToReturn.Add(new SystemIconItem
        {
            Id = "sys_folder",
            Title = "Downloads Folder",
            LibraryPath = "C:\\imageres.dll",
            IconIndex = 3,
            Category = "Folders"
        });
        mockLib.SystemIconsToReturn.Add(new SystemIconItem
        {
            Id = "sys_device",
            Title = "Audio Device",
            LibraryPath = "C:\\imageres.dll",
            IconIndex = 14,
            Category = "Devices"
        });

        var vm = new IconsViewModel(mockService, mockPrefs, mockPicker, mockErrors, mockLib);
        await vm.OpenLibraryAsync();

        // 1. Search matching title
        vm.LibrarySearchQuery = "Downloads";
        Assert.Single(vm.DisplayedSystemIcons);
        Assert.Equal(1, vm.LibraryDisplayedCount);

        // 2. Combine with mismatched category -> results should be empty (count = 0)
        vm.SelectCategory("Devices");
        Assert.Empty(vm.DisplayedSystemIcons);
        Assert.Equal(0, vm.LibraryDisplayedCount);

        // 3. Reset filters -> restores all items
        vm.ResetLibraryFilters();
        Assert.Equal(2, vm.DisplayedSystemIcons.Count);
        Assert.Equal(2, vm.LibraryDisplayedCount);
    }
}