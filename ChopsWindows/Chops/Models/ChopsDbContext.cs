using Microsoft.EntityFrameworkCore;
using Chops.Models;

namespace Chops;

/// <summary>
/// Entity Framework Core database context for Chops.
/// Replaces SwiftData ModelContainer from the macOS version.
/// Stores data in a SQLite database in the user's AppData folder.
/// </summary>
public class ChopsDbContext : DbContext
{
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillCollection> Collections => Set<SkillCollection>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDir = Path.Combine(appData, "Chops");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "chops.db");
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasIndex(s => s.ResolvedPath).IsUnique();
            entity.HasMany(s => s.Collections)
                  .WithMany(c => c.Skills);
        });

        modelBuilder.Entity<SkillCollection>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });
    }
}
