using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AINovelWriter.Services;
using AINovelWriter.ViewModels;

namespace AINovelWriter.Views;

public sealed partial class CreateWorkPage : Page
{
    public CreateWorkViewModel Vm { get; } = new();

    public CreateWorkPage()
    {
        InitializeComponent();
    }

    private void OnBack(object sender, RoutedEventArgs e) => AppState.GoBack();

    private void OnCreateAndEdit(object sender, RoutedEventArgs e)
    {
        AppState.CurrentProject = Vm.Build();
        AppState.Navigate(typeof(EditorPage));
    }

    private void OnCreateAndOutline(object sender, RoutedEventArgs e)
    {
        AppState.CurrentProject = Vm.Build();
        AppState.Navigate(typeof(OutlinePage));
    }
}
