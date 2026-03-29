namespace Chops.Utilities;

/// <summary>
/// Parses Cursor <c>.mdc</c> rule files.
/// These use the same frontmatter format as <c>.md</c> skill files,
/// so this simply delegates to <see cref="FrontmatterParser"/>.
/// Mirrors <c>MDCParser.swift</c> from the macOS version.
/// </summary>
public static class MDCParser
{
    public static ParsedSkill Parse(string text) => FrontmatterParser.Parse(text);
}
