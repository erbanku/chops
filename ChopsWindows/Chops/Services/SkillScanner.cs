using System.Text.Json;
using Chops.Models;
using Chops.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Chops.Services;

/// <summary>
/// Scans tool directories to discover skills and agents, then persists them to the database.
/// Mirrors <c>SkillScanner.swift</c> from the macOS version.
/// </summary>
public class SkillScanner
{
    /// <summary>
    /// Intermediate data collected from the filesystem before database upsert.
    /// </summary>
    public record ScannedSkillData(
        string FilePath,
        string ResolvedPath,
        ToolSource ToolSource,
        bool IsDirectory,
        bool IsGlobal,
        string Name,
        string? Description,
        string Content,
        Dictionary<string, string> Frontmatter,
        DateTime ModDate,
        long FileSize,
        ItemKind Kind
    );

    /// <summary>
    /// Filenames to ignore during scanning (README, LICENSE, etc.).
    /// </summary>
    private static readonly HashSet<string> IgnoredFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md", "README", "CLAUDE.md", "AGENTS.md", "AGENTS.override.md",
        "global_rules.md", "SYSTEM.md", "APPEND_SYSTEM.md", "LICENSE.md", "LICENSE", "CHANGELOG.md"
    };

    /// <summary>
    /// Valid skill file extensions.
    /// </summary>
    private static readonly HashSet<string> SkillExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".mdc", ".toml"
    };

    private CancellationTokenSource? _scanCts;

    /// <summary>
    /// Runs a full scan of all installed tools and persists results to the database.
    /// Cancels any in-progress scan before starting.
    /// </summary>
    public async Task ScanAllAsync()
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        var scanned = await Task.Run(() => CollectAllSkills(token), token);

        if (token.IsCancellationRequested)
            return;

        await ApplyResultsAsync(scanned);
    }

    /// <summary>
    /// Scans the filesystem for all skills across installed tools.
    /// Pure I/O — no database access.
    /// </summary>
    private List<ScannedSkillData> CollectAllSkills(CancellationToken token)
    {
        var results = new List<ScannedSkillData>();

        foreach (ToolSource tool in Enum.GetValues<ToolSource>())
        {
            if (token.IsCancellationRequested) break;

            foreach (var globalPath in tool.GlobalPaths())
            {
                if (token.IsCancellationRequested) break;

                if (!Directory.Exists(globalPath))
                    continue;

                ScanDirectory(globalPath, tool, isGlobal: true, results, token);
            }
        }

        return results;
    }

    /// <summary>
    /// Scans a single directory for skill/agent files.
    /// </summary>
    private void ScanDirectory(string dirPath, ToolSource tool, bool isGlobal,
        List<ScannedSkillData> results, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        try
        {
            // Check for directory-based skills (skillname/SKILL.md or AGENTS.md)
            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                if (token.IsCancellationRequested) break;

                var skillFile = Path.Combine(subDir, "SKILL.md");
                var agentFile = Path.Combine(subDir, "AGENTS.md");

                if (File.Exists(skillFile))
                {
                    var data = ScanFile(skillFile, tool, isGlobal, isDirectory: true, ItemKind.Skill);
                    if (data is not null) results.Add(data);
                }
                else if (File.Exists(agentFile))
                {
                    var data = ScanFile(agentFile, tool, isGlobal, isDirectory: true, ItemKind.Agent);
                    if (data is not null) results.Add(data);
                }
            }

            // Check for loose skill files
            foreach (var file in Directory.GetFiles(dirPath))
            {
                if (token.IsCancellationRequested) break;

                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(file).ToLowerInvariant();

                if (IgnoredFileNames.Contains(fileName))
                    continue;

                if (!SkillExtensions.Contains(extension))
                    continue;

                // Determine kind from path
                var kind = file.Contains("agents", StringComparison.OrdinalIgnoreCase)
                    ? ItemKind.Agent
                    : ItemKind.Skill;

                var data = ScanFile(file, tool, isGlobal, isDirectory: false, kind);
                if (data is not null) results.Add(data);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have permission to read
        }
        catch (DirectoryNotFoundException)
        {
            // Directory was deleted between check and scan
        }
    }

    /// <summary>
    /// Parses a single skill file into scanned data.
    /// </summary>
    private static ScannedSkillData? ScanFile(string filePath, ToolSource tool,
        bool isGlobal, bool isDirectory, ItemKind kind)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) return null;

            // Resolve symlinks by following the full chain
            var resolvedPath = filePath;
            try
            {
                var target = fileInfo.LinkTarget;
                while (target is not null)
                {
                    if (!Path.IsPathRooted(target))
                        target = Path.GetFullPath(target, Path.GetDirectoryName(resolvedPath)!);
                    resolvedPath = target;
                    var nextInfo = new FileInfo(resolvedPath);
                    target = nextInfo.LinkTarget;
                }
                resolvedPath = Path.GetFullPath(resolvedPath);
            }
            catch
            {
                resolvedPath = fileInfo.FullName;
            }

            var parsed = SkillParser.Parse(filePath, tool);
            if (parsed is null) return null;

            var name = parsed.Name ?? Path.GetFileNameWithoutExtension(filePath);

            return new ScannedSkillData(
                FilePath: filePath,
                ResolvedPath: resolvedPath,
                ToolSource: tool,
                IsDirectory: isDirectory,
                IsGlobal: isGlobal,
                Name: name,
                Description: parsed.Description,
                Content: parsed.Content,
                Frontmatter: parsed.Frontmatter,
                ModDate: fileInfo.LastWriteTimeUtc,
                FileSize: fileInfo.Length,
                Kind: kind
            );
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Persists scanned skill data to the database.
    /// Groups by resolved path (deduplication), merges installations,
    /// and removes skills no longer found on disk.
    /// </summary>
    private static async Task ApplyResultsAsync(List<ScannedSkillData> scanned)
    {
        await using var db = new ChopsDbContext();

        // Group by resolved path for deduplication
        var grouped = scanned
            .GroupBy(s => s.ResolvedPath)
            .ToDictionary(g => g.Key, g => g.ToList());

        var existingSkills = await db.Skills.ToListAsync();
        var existingByPath = existingSkills.ToDictionary(s => s.ResolvedPath);
        var seenPaths = new HashSet<string>();

        foreach (var (resolvedPath, entries) in grouped)
        {
            seenPaths.Add(resolvedPath);
            var primary = entries[0];

            if (existingByPath.TryGetValue(resolvedPath, out var existing))
            {
                // Update existing skill
                existing.Name = primary.Name;
                existing.Description = primary.Description;
                existing.Content = primary.Content;
                existing.FrontmatterJson = JsonSerializer.Serialize(primary.Frontmatter);
                existing.FileModifiedDate = primary.ModDate;
                existing.FileSize = primary.FileSize;
                existing.Kind = primary.Kind.ToString().ToLowerInvariant();
                existing.IsGlobal = primary.IsGlobal;
                existing.ToolSourcesRaw = string.Join(",",
                    entries.Select(e => e.ToolSource.ToString().ToLowerInvariant()).Distinct());
                existing.InstalledPathsJson = JsonSerializer.Serialize(
                    entries.Select(e => e.FilePath).Distinct().ToList());
            }
            else
            {
                // Create new skill
                var skill = new Skill
                {
                    ResolvedPath = resolvedPath,
                    FilePath = primary.FilePath,
                    IsDirectory = primary.IsDirectory,
                    Name = primary.Name,
                    Description = primary.Description,
                    Content = primary.Content,
                    FrontmatterJson = JsonSerializer.Serialize(primary.Frontmatter),
                    FileModifiedDate = primary.ModDate,
                    FileSize = primary.FileSize,
                    IsGlobal = primary.IsGlobal,
                    Kind = primary.Kind.ToString().ToLowerInvariant(),
                    ToolSourcesRaw = string.Join(",",
                        entries.Select(e => e.ToolSource.ToString().ToLowerInvariant()).Distinct()),
                    InstalledPathsJson = JsonSerializer.Serialize(
                        entries.Select(e => e.FilePath).Distinct().ToList())
                };
                db.Skills.Add(skill);
            }
        }

        // Remove skills no longer on disk
        var toRemove = existingSkills.Where(s => !seenPaths.Contains(s.ResolvedPath)).ToList();
        db.Skills.RemoveRange(toRemove);

        await db.SaveChangesAsync();
    }
}
