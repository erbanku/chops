using Chops.Models;
using Chops.Utilities;

namespace Chops.Services;

/// <summary>
/// Routes skill files to the appropriate parser based on file extension and tool source.
/// Mirrors <c>SkillParser.swift</c> from the macOS version.
/// </summary>
public static class SkillParser
{
    /// <summary>
    /// Parses a skill file and returns structured metadata.
    /// Routes <c>.mdc</c> files through <see cref="MDCParser"/> and everything else
    /// through <see cref="FrontmatterParser"/>.
    /// </summary>
    public static ParsedSkill? Parse(string filePath, ToolSource toolSource)
    {
        if (!File.Exists(filePath))
            return null;

        string text;
        try
        {
            text = File.ReadAllText(filePath);
        }
        catch
        {
            return null;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // .mdc files (Cursor rules) use the MDC parser
        if (extension == ".mdc")
            return MDCParser.Parse(text);

        // Try frontmatter parser first
        var result = FrontmatterParser.Parse(text);

        // If no frontmatter was found, try heading-based format
        if (result.Name is null && result.Frontmatter.Count == 0)
        {
            var lines = text.Split('\n');
            var headingLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("# "));
            if (headingLine is not null)
            {
                var name = headingLine.TrimStart()[2..].Trim();
                return new ParsedSkill([], text, name, null);
            }
        }

        return result;
    }
}
