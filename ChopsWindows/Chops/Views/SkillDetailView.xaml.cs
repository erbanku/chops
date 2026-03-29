using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Chops.Views;

/// <summary>
/// Detail pane showing a selected skill's content with edit and preview modes.
/// Mirrors <c>SkillDetailView.swift</c> from the macOS version.
/// Supports Ctrl+S to save and toggle between editor and markdown preview.
/// </summary>
public sealed partial class SkillDetailView : UserControl
{
    private bool _isEditing;

    public SkillDetailView()
    {
        InitializeComponent();
    }

    private void EditToggle_Click(object sender, RoutedEventArgs e)
    {
        _isEditing = !_isEditing;

        EditorTextBox.Visibility = _isEditing ? Visibility.Visible : Visibility.Collapsed;
        PreviewWebView.Visibility = _isEditing ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Save the current editor content back to the skill file
        var content = EditorTextBox.Text;
        if (string.IsNullOrEmpty(content))
            return;

        // File save will be wired through the ViewModel/AppState
        await Task.CompletedTask;
    }
}
