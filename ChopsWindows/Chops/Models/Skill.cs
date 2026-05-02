using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Chops.Models;

/// <summary>
/// Represents a discovered skill or agent file. Uniquely identified by its resolved path.
/// Mirrors the Swift <c>Skill</c> SwiftData model.
/// </summary>
public class Skill
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Resolved symlink path — the unique identifier for deduplication.
    /// </summary>
    [Required]
    public string ResolvedPath { get; set; } = string.Empty;

    /// <summary>
    /// Original file path on disk.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether the skill is a directory-based skill (e.g., skillname/SKILL.md).
    /// </summary>
    public bool IsDirectory { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Full file content (markdown/mdc).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// JSON-encoded frontmatter key-value pairs.
    /// </summary>
    public string? FrontmatterJson { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime? LastOpened { get; set; }

    public DateTime? FileModifiedDate { get; set; }

    public long FileSize { get; set; }

    public bool IsGlobal { get; set; }

    /// <summary>
    /// Discriminator: "skill" or "agent".
    /// </summary>
    public string Kind { get; set; } = "skill";

    /// <summary>
    /// Comma-separated tool source raw values (e.g., "claude,cursor").
    /// </summary>
    public string ToolSourcesRaw { get; set; } = string.Empty;

    /// <summary>
    /// JSON-encoded array of all filesystem paths where this skill is installed.
    /// </summary>
    public string? InstalledPathsJson { get; set; }

    // Navigation property
    public ICollection<SkillCollection> Collections { get; set; } = [];

    // Computed properties

    [NotMapped]
    public ItemKind ItemKind => Kind == "agent" ? ItemKind.Agent : ItemKind.Skill;

    [NotMapped]
    public IReadOnlyList<ToolSource> ToolSources =>
        string.IsNullOrEmpty(ToolSourcesRaw)
            ? []
            : ToolSourcesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(raw => Enum.TryParse<ToolSource>(raw.Trim(), ignoreCase: true, out var ts) ? ts : (ToolSource?)null)
                .Where(ts => ts.HasValue)
                .Select(ts => ts!.Value)
                .ToList();

    [NotMapped]
    public ToolSource? PrimaryToolSource => ToolSources.FirstOrDefault();

    [NotMapped]
    public IReadOnlyList<string> InstalledPaths
    {
        get
        {
            if (string.IsNullOrEmpty(InstalledPathsJson))
                return [];
            try
            {
                return JsonSerializer.Deserialize<List<string>>(InstalledPathsJson) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    [NotMapped]
    public Dictionary<string, string> Frontmatter
    {
        get
        {
            if (string.IsNullOrEmpty(FrontmatterJson))
                return [];
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(FrontmatterJson) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    [NotMapped]
    public int InstallCount => ToolSources.Count;

    [NotMapped]
    public string DisplayTypeName => ItemKind == ItemKind.Agent ? "Agent" : "Skill";

    /// <summary>
    /// Extracts the project name from the file path.
    /// For example, "C:\Users\user\Development\my-project\.claude\skills\foo" → "my-project".
    /// </summary>
    [NotMapped]
    public string? ProjectName
    {
        get
        {
            var parts = FilePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith('.') && i > 0)
                    return parts[i - 1];
            }
            return null;
        }
    }

    /// <summary>
    /// Merges another installation path and tool source into this skill.
    /// Used during scanning to combine the same skill found in multiple tool directories.
    /// </summary>
    public void AddInstallation(string path, ToolSource tool)
    {
        var paths = InstalledPaths.ToList();
        if (!paths.Contains(path))
        {
            paths.Add(path);
            InstalledPathsJson = JsonSerializer.Serialize(paths);
        }

        var sources = ToolSources.ToList();
        if (!sources.Contains(tool))
        {
            sources.Add(tool);
            ToolSourcesRaw = string.Join(",", sources.Select(s => s.ToString().ToLowerInvariant()));
        }
    }

    /// <summary>
    /// Deletes the skill file(s) from disk.
    /// </summary>
    public void DeleteFromDisk()
    {
        foreach (var path in InstalledPaths)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
            else if (File.Exists(path))
                File.Delete(path);
        }
    }
}

public enum ItemKind
{
    Skill,
    Agent
}
