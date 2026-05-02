using Microsoft.UI.Xaml.Controls;

namespace Chops.Views;

/// <summary>
/// Displays the filtered list of skills/agents in the center column.
/// Mirrors <c>SkillListView.swift</c> from the macOS version.
/// </summary>
public sealed partial class SkillListView : UserControl
{
    public SkillListView()
    {
        InitializeComponent();
    }

    private void SkillsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Skill selection is propagated through data binding to AppState.SelectedSkill
    }
}
