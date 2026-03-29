namespace Chops.Models;

/// <summary>
/// Enumerates all supported AI coding tools and their filesystem paths on Windows.
/// Mirrors the Swift <c>ToolSource</c> enum.
/// </summary>
public enum ToolSource
{
    Claude,
    Cursor,
    Windsurf,
    Codex,
    Copilot,
    Aider,
    Amp,
    OpenClaw,
    OpenCode,
    Pi,
    Agents,
    Augment,
    Antigravity,
    ClaudeDesktop,
    Custom
}

public static class ToolSourceExtensions
{
    private static string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// Whether the tool appears in the sidebar filter list.
    /// </summary>
    public static bool IsListable(this ToolSource source) => source switch
    {
        ToolSource.Custom or ToolSource.OpenClaw or ToolSource.ClaudeDesktop or ToolSource.Agents or ToolSource.Aider => false,
        _ => true
    };

    /// <summary>
    /// Human-readable display name for each tool.
    /// </summary>
    public static string DisplayName(this ToolSource source) => source switch
    {
        ToolSource.Claude => "Claude Code",
        ToolSource.Cursor => "Cursor",
        ToolSource.Windsurf => "Windsurf",
        ToolSource.Codex => "Codex",
        ToolSource.Copilot => "Copilot",
        ToolSource.Aider => "Aider",
        ToolSource.Amp => "Amp",
        ToolSource.OpenClaw => "OpenClaw",
        ToolSource.OpenCode => "OpenCode",
        ToolSource.Pi => "Pi",
        ToolSource.Agents => "Global Agents",
        ToolSource.Augment => "Augment",
        ToolSource.Antigravity => "Antigravity",
        ToolSource.ClaudeDesktop => "Claude Desktop",
        ToolSource.Custom => "Custom",
        _ => source.ToString()
    };

    /// <summary>
    /// Segoe Fluent icon glyph for each tool (used in WinUI).
    /// Falls back to generic icons where tool-specific ones aren't available.
    /// </summary>
    public static string IconGlyph(this ToolSource source) => source switch
    {
        ToolSource.Claude => "\uE943",        // Robot
        ToolSource.Cursor => "\uE790",        // Keyboard
        ToolSource.Windsurf => "\uE774",       // Globe
        ToolSource.Codex => "\uE756",         // Code
        ToolSource.Copilot => "\uE945",        // People
        ToolSource.Aider => "\uE771",         // Settings
        ToolSource.Amp => "\uE945",           // People
        ToolSource.OpenClaw => "\uE774",       // Globe
        ToolSource.OpenCode => "\uE756",       // Code
        ToolSource.Pi => "\uE946",            // Math
        ToolSource.Agents => "\uE716",         // World
        ToolSource.Augment => "\uE8F1",        // Repair
        ToolSource.Antigravity => "\uE8AB",    // Up arrow
        ToolSource.ClaudeDesktop => "\uE943",  // Robot
        ToolSource.Custom => "\uE8B7",         // Folder
        _ => "\uE8B7"
    };

    /// <summary>
    /// Returns the global skill directory paths for this tool on Windows.
    /// Uses <c>%USERPROFILE%</c> as the home directory equivalent.
    /// </summary>
    public static IReadOnlyList<string> GlobalPaths(this ToolSource source) => source switch
    {
        ToolSource.Claude =>
        [
            Path.Combine(UserProfile, ".claude", "skills"),
            Path.Combine(UserProfile, ".claude", "agents")
        ],
        ToolSource.Cursor =>
        [
            Path.Combine(UserProfile, ".cursor", "skills"),
            Path.Combine(UserProfile, ".cursor", "rules"),
            Path.Combine(UserProfile, ".cursor", "agents")
        ],
        ToolSource.Windsurf =>
        [
            Path.Combine(UserProfile, ".codeium", "windsurf", "memories"),
            Path.Combine(UserProfile, ".windsurf", "rules")
        ],
        ToolSource.Codex =>
        [
            Path.Combine(UserProfile, ".codex", "skills"),
            Path.Combine(UserProfile, ".codex", "agents")
        ],
        ToolSource.Amp =>
        [
            Path.Combine(UserProfile, ".config", "amp", "skills")
        ],
        ToolSource.OpenCode =>
        [
            Path.Combine(UserProfile, ".opencode", "skills")
        ],
        ToolSource.Agents =>
        [
            Path.Combine(UserProfile, ".agents", "skills")
        ],
        _ => []
    };

    /// <summary>
    /// Returns the global agent directory paths for this tool on Windows.
    /// </summary>
    public static IReadOnlyList<string> GlobalAgentPaths(this ToolSource source) => source switch
    {
        ToolSource.Claude => [Path.Combine(UserProfile, ".claude", "agents")],
        ToolSource.Cursor => [Path.Combine(UserProfile, ".cursor", "agents")],
        ToolSource.Codex => [Path.Combine(UserProfile, ".codex", "agents")],
        _ => []
    };

    /// <summary>
    /// Checks whether the tool appears to be installed on this Windows system.
    /// Looks for config directories, CLI binaries, and AppData entries.
    /// </summary>
    public static bool IsInstalled(this ToolSource source)
    {
        // Check if any of the global paths exist
        foreach (var path in source.GlobalPaths())
        {
            if (Directory.Exists(path))
                return true;
        }

        // Check for CLI binaries on PATH or common locations
        var binaryName = source switch
        {
            ToolSource.Claude => "claude.exe",
            ToolSource.Cursor => "cursor.exe",
            ToolSource.Codex => "codex.exe",
            ToolSource.Aider => "aider.exe",
            ToolSource.Amp => "amp.exe",
            _ => null
        };

        if (binaryName is not null)
        {
            // Check PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                if (File.Exists(Path.Combine(dir, binaryName)))
                    return true;
            }

            // Check common Windows install locations
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            if (File.Exists(Path.Combine(localAppData, "Programs", source.ToString(), binaryName)))
                return true;
            if (File.Exists(Path.Combine(programFiles, source.ToString(), binaryName)))
                return true;
        }

        // Check for app in Start Menu / AppData
        var appDirs = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };

        foreach (var appDir in appDirs)
        {
            var toolDir = Path.Combine(appDir, source.DisplayName());
            if (Directory.Exists(toolDir))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Project-level subdirectories to probe when scanning a development project.
    /// Mirrors the macOS scanner's project probes.
    /// </summary>
    public static IReadOnlyList<string> ProjectProbes(this ToolSource source) => source switch
    {
        ToolSource.Claude => [".claude\\skills", ".claude\\agents"],
        ToolSource.Cursor => [".cursor\\skills", ".cursor\\rules", ".cursor\\agents"],
        ToolSource.Codex => [".codex\\skills", ".codex\\agents"],
        ToolSource.Windsurf => [".windsurf\\rules"],
        ToolSource.Copilot => [".github", ".github\\agents"],
        ToolSource.Amp => [".config\\amp\\skills"],
        ToolSource.OpenCode => [".opencode\\skills"],
        _ => []
    };
}
