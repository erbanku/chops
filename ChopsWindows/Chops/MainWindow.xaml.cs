using Microsoft.UI.Xaml;

namespace Chops;

/// <summary>
/// Main application window with three-column layout matching the macOS NavigationSplitView:
/// Sidebar → Skill List → Detail.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Chops";
        ExtendsContentIntoTitleBar = true;
    }
}
