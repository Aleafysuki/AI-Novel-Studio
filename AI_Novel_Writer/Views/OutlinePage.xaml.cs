using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AINovelWriter.Models;
using AINovelWriter.ViewModels;

namespace AINovelWriter.Views;

public sealed partial class OutlinePage : Page
{
    public OutlineViewModel Vm { get; } = new();

    private OutlineNode? _editingNode;

    public OutlinePage()
    {
        InitializeComponent();
        Loaded += (_, _) => NodeDialog.XamlRoot = this.Content.XamlRoot;
    }

    private void OnToggleConfirm(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is OutlineNode n)
            Vm.ToggleConfirm(n);
    }

    private async void OnGenerateBody(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is OutlineNode n)
            await Vm.GenerateBodyAsync(n);
    }

    private void OnOpenInEditor(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is OutlineNode n)
            Vm.OpenInEditor(n);
    }

    private void OnEditNode(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is OutlineNode n)
        {
            _editingNode = n;
            NodeTitle.Text = n.Title;
            NodeSummary.Text = n.Summary;
            NodeKey.Text = n.KeyCharacters;
            _ = NodeDialog.ShowAsync();
        }
    }

    private void OnNodeDialogSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_editingNode == null) return;
        _editingNode.Title = string.IsNullOrWhiteSpace(NodeTitle.Text) ? _editingNode.Title : NodeTitle.Text.Trim();
        _editingNode.Summary = NodeSummary.Text;
        _editingNode.KeyCharacters = NodeKey.Text;
        _editingNode = null;
    }

    private void OnHome(object sender, RoutedEventArgs e) => Vm.GoHome();
}
