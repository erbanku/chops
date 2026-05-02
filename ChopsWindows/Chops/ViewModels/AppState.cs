using Chops.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chops.ViewModels;

/// <summary>
/// Observable application state shared across all views.
/// Mirrors <c>AppState</c> from the macOS version, using CommunityToolkit.Mvvm
/// for property change notifications (equivalent to Swift's <c>@Observable</c>).
/// </summary>
public partial class AppState : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ToolSource? _selectedToolFilter;

    [ObservableProperty]
    private Skill? _selectedSkill;

    [ObservableProperty]
    private SkillCollection? _selectedCollection;

    [ObservableProperty]
    private ItemKind? _selectedKindFilter;

    [ObservableProperty]
    private bool _isScanning;
}
