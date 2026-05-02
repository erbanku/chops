using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;

namespace Chops;

/// <summary>
/// Application entry point. Sets up the database and creates the main window.
/// Mirrors ChopsApp.swift from the macOS version.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        EnsureDatabase();

        _window = new MainWindow();
        _window.Activate();
    }

    private static void EnsureDatabase()
    {
        using var db = new ChopsDbContext();
        db.Database.Migrate();
    }
}
