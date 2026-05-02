using System.ComponentModel.DataAnnotations;

namespace Chops.Models;

/// <summary>
/// A user-created grouping of skills.
/// Mirrors the Swift <c>SkillCollection</c> SwiftData model.
/// </summary>
public class SkillCollection
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = "folder";

    public int SortOrder { get; set; }

    // Navigation property
    public ICollection<Skill> Skills { get; set; } = [];
}
