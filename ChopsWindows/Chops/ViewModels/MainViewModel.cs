using System.Collections.ObjectModel;
using Chops.Models;
using Chops.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace Chops.ViewModels;

/// <summary>
/// Main view model coordinating skill scanning, filtering, and selection.
/// Manages the lifecycle of <see cref="SkillScanner"/> and <see cref="FileWatcher"/>.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SkillScanner _scanner = new();
    private readonly FileWatcher _fileWatcher;

    [ObservableProperty]
    private AppState _appState = new();

    public ObservableCollection<Skill> Skills { get; } = [];
    public ObservableCollection<SkillCollection> Collections { get; } = [];
    public ObservableCollection<ToolSource> InstalledTools { get; } = [];

    public MainViewModel()
    {
        _fileWatcher = new FileWatcher(_ => _ = RefreshAsync());
    }

    /// <summary>
    /// Initializes the view model: scans for skills, loads collections,
    /// detects installed tools, and starts file watching.
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        await RefreshAsync();
        StartWatching();
    }

    /// <summary>
    /// Re-scans the filesystem and refreshes the in-memory collections from the database.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        AppState.IsScanning = true;

        try
        {
            await _scanner.ScanAllAsync();
            await LoadFromDatabaseAsync();
            DetectInstalledTools();
        }
        finally
        {
            AppState.IsScanning = false;
        }
    }

    /// <summary>
    /// Returns skills filtered by the current search text, tool filter, and kind filter.
    /// </summary>
    public IEnumerable<Skill> FilteredSkills
    {
        get
        {
            var query = Skills.AsEnumerable();

            if (AppState.SelectedToolFilter is { } tool)
                query = query.Where(s => s.ToolSources.Contains(tool));

            if (AppState.SelectedKindFilter is { } kind)
                query = query.Where(s => s.ItemKind == kind);

            if (AppState.SelectedCollection is { } collection)
                query = query.Where(s => s.Collections.Any(c => c.Id == collection.Id));

            if (!string.IsNullOrWhiteSpace(AppState.SearchText))
            {
                var search = AppState.SearchText;
                query = query.Where(s =>
                    s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (s.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    s.Content.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(s => s.Name);
        }
    }

    private async Task LoadFromDatabaseAsync()
    {
        await using var db = new ChopsDbContext();

        var skills = await db.Skills
            .Include(s => s.Collections)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var collections = await db.Collections
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        Skills.Clear();
        foreach (var skill in skills)
            Skills.Add(skill);

        Collections.Clear();
        foreach (var collection in collections)
            Collections.Add(collection);
    }

    private void DetectInstalledTools()
    {
        InstalledTools.Clear();
        foreach (var tool in Enum.GetValues<ToolSource>())
        {
            if (tool.IsListable() && tool.IsInstalled())
                InstalledTools.Add(tool);
        }
    }

    private void StartWatching()
    {
        var paths = Enum.GetValues<ToolSource>()
            .SelectMany(t => t.GlobalPaths())
            .Where(Directory.Exists)
            .ToList();

        _fileWatcher.WatchDirectories(paths);
    }

    public void Dispose()
    {
        _fileWatcher.Dispose();
        GC.SuppressFinalize(this);
    }
}
