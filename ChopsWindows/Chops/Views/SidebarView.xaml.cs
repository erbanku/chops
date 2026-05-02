using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Chops.Views;

/// <summary>
/// Sidebar with tool filters, search box, and collections list.
/// Mirrors <c>SidebarView.swift</c> from the macOS version.
/// </summary>
public sealed partial class SidebarView : UserControl
{
    public SidebarView()
    {
        InitializeComponent();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Search text changes are propagated through data binding to AppState.SearchText
    }

    private void ToolFilterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Tool filter selection is propagated through data binding to AppState.SelectedToolFilter
    }

    private void CollectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Collection selection is propagated through data binding to AppState.SelectedCollection
    }
}
