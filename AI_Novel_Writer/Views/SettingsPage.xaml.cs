using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AINovelWriter.Models;
using AINovelWriter.Services;

namespace AINovelWriter.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ApiKeyBox.Password = AppState.Settings.ApiKey;
        };
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (AppState.RootFrame?.CanGoBack == true) AppState.RootFrame.GoBack();
        else AppState.Navigate(typeof(HomePage));
    }

    private void OnAddStyle(object sender, RoutedEventArgs e)
    {
        var s = NewStyleBox.Text.Trim();
        if (string.IsNullOrEmpty(s)) return;
        if (!AppState.Settings.CustomStyles.Contains(s))
            AppState.Settings.CustomStyles.Add(s);
        NewStyleBox.Text = "";
    }

    private void OnRemoveStyle(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is string s)
            AppState.Settings.CustomStyles.Remove(s);
    }

    private void OnResetTemplate(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is PromptTemplateItem item)
            item.Template = AppSettings.DefaultTemplate(item.Kind);
    }

    private void OnAddPreset(object sender, RoutedEventArgs e)
    {
        var name = NewPresetName.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;
        if (AppState.Settings.UserPresets.Any(p => p.Name == name))
        {
            NewPresetName.Text = "";
            return;
        }
        AppState.Settings.UserPresets.Add(new UserPreset { Name = name, Content = NewPresetContent.Text });
        NewPresetName.Text = "";
        NewPresetContent.Text = "";
    }

    private void OnRemovePreset(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is UserPreset p)
            AppState.Settings.UserPresets.Remove(p);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        AppState.Settings.ApiKey = ApiKeyBox.Password;
        SettingsService.Save();
        App.ApplyTheme();
        if (AppState.RootFrame?.CanGoBack == true) AppState.RootFrame.GoBack();
        else AppState.Navigate(typeof(HomePage));
    }
}
