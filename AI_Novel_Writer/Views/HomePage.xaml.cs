using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AINovelWriter.Services;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AINovelWriter.Views;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await CheckAutoRecoverAsync();
    }

    /// <summary>
    /// 检查自动暂存备份，询问用户是否恢复
    /// </summary>
    private async Task CheckAutoRecoverAsync()
    {
        var backupIds = AutoSaveService.GetBackupProjectIds();
        if (backupIds.Length == 0) return;

        foreach (var projectId in backupIds)
        {
            var project = await AutoSaveService.LoadBackupAsync(projectId);
            if (project == null) continue;

            var dialog = new ContentDialog
            {
                Title = "发现自动暂存",
                Content = $"检测到项目「{project.Title}」有自动暂存的备份数据" +
                          $"\n（最后修改：{project.LastModifiedAt:yyyy-MM-dd HH:mm}）" +
                          $"\n\n可能是上次异常退出时自动保存的。是否恢复？",
                PrimaryButtonText = "恢复并打开",
                SecondaryButtonText = "删除备份",
                CloseButtonText = "忽略",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            switch (result)
            {
                case ContentDialogResult.Primary:
                    AppState.CurrentProject = project;
                    AppState.CurrentProjectPath = null;
                    AppState.HasUnsavedChanges = false;
                    AppState.Navigate(typeof(EditorPage));
                    return;
                case ContentDialogResult.Secondary:
                    AutoSaveService.DeleteBackup(projectId);
                    break;
                // 忽略：不动备份
            }
        }
    }

    private void OnCreateWork(object sender, RoutedEventArgs e)
        => AppState.Navigate(typeof(CreateWorkPage));

    private void OnOutlineMode(object sender, RoutedEventArgs e)
    {
        // 大纲模式需要一个项目上下文；若没有则先用示例项目
        AppState.CurrentProject ??= AppState.ProjectService.CreateSampleProject();
        AppState.Navigate(typeof(OutlinePage));
    }

    private void OnOpenSample(object sender, RoutedEventArgs e)
    {
        AppState.CurrentProject = AppState.ProjectService.CreateSampleProject();
        AppState.CurrentProjectPath = null;
        AppState.Navigate(typeof(EditorPage));
    }

    private void OnSettings(object sender, RoutedEventArgs e)
        => AppState.Navigate(typeof(SettingsPage));

    private async void OnOpenProject(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add(".ainovel");
        InitializeWithWindow.Initialize(picker, App.WindowHandle);
        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        var project = AppState.ProjectService.LoadFromFile(file);
        if (project == null)
        {
            // 简单提示：通过状态文本不便，这里用 ContentDialog
            var d = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "打开失败",
                Content = "无法解析该项目文件，可能格式不正确或已损坏。",
                CloseButtonText = "确定"
            };
            _ = d.ShowAsync();
            return;
        }
        AppState.CurrentProject = project;
        AppState.CurrentProjectPath = file.Path;
        AppState.Navigate(typeof(EditorPage));
    }
}
