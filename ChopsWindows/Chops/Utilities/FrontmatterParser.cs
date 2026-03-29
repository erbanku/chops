namespace Chops.Utilities;

/// <summary>
/// Result of parsing a skill file's frontmatter and content.
/// </summary>
public record ParsedSkill(
    Dictionary<string, string> Frontmatter,
    string Content,
    string? Name,
    string? Description
);

/// <summary>
/// Parses YAML-style frontmatter from markdown skill files.
/// Mirrors <c>FrontmatterParser.swift</c> from the macOS version.
/// </summary>
public static class FrontmatterParser
{
    /// <summary>
    /// Parses a markdown file's text, extracting the optional YAML frontmatter block
    /// delimited by <c>---</c> lines and the remaining body content.
    /// </summary>
    public static ParsedSkill Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ParsedSkill([], string.Empty, null, null);

        var lines = text.Split('\n');

        // Check for opening ---
        if (lines.Length == 0 || lines[0].Trim() != "---")
            return new ParsedSkill([], text, null, null);

        var frontmatter = new Dictionary<string, string>();
        int closingIndex = -1;

        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                closingIndex = i;
                break;
            }

            var colonIndex = lines[i].IndexOf(':');
            if (colonIndex > 0)
            {
                var key = lines[i][..colonIndex].Trim();
                var value = lines[i][(colonIndex + 1)..].Trim();
                frontmatter[key] = value;
            }
        }

        if (closingIndex < 0)
            return new ParsedSkill([], text, null, null);

        var content = string.Join('\n', lines.Skip(closingIndex + 1)).TrimStart('\n');

        frontmatter.TryGetValue("name", out var name);
        frontmatter.TryGetValue("description", out var description);

        return new ParsedSkill(frontmatter, content, name, description);
    }
}
