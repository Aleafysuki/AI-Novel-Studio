using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AINovelWriter.Views;
using AINovelWriter.Services;
using Microsoft.UI.Windowing;

namespace AINovelWriter;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // 注入导航中枢并进入首页
        AppState.RootFrame = RootFrame;
        RootFrame.Navigate(typeof(HomePage));

        // 注册窗口关闭事件，检查未保存的变更
        AppWindow.Closing += OnWindowClosing;
    }

    private async void OnWindowClosing(object sender, AppWindowClosingEventArgs e)
    {
        if (!AppState.HasUnsavedChanges) return;

        // 阻止窗口立即关闭
        e.Cancel = true;

        // 尝试获取当前页面的 XamlRoot
        var currentPage = (RootFrame?.Content as UIElement);
        var xamlRoot = currentPage?.XamlRoot;
        if (xamlRoot == null) return; // 没有有效页面，允许后续尝试关闭

        var dialog = new ContentDialog
        {
            Title = "未保存的更改",
            Content = "当前项目有未保存的更改。\n\n自动暂存功能已开启，修改保存在备份文件中。\n下次打开时会询问是否恢复。",
            PrimaryButtonText = "保存并退出",
            SecondaryButtonText = "不保存，直接退出",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();

        switch (result)
        {
            case ContentDialogResult.Primary:
                // 保存：触发当前 ViewModel 的保存
                if (RootFrame?.Content is Views.EditorPage editorPage)
                {
                    editorPage.Vm.Save();
                    // 写入正式文件
                    var filePath = AppState.CurrentProjectPath;
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        try
                        {
                            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                            AppState.ProjectService.SaveToFile(editorPage.Vm.Project, file);
                        }
                        catch { /* 保存失败不阻止退出 */ }
                    }
                }
                // 重置标记以避免递归关闭
                AppState.HasUnsavedChanges = false;
                Close();
                break;

            case ContentDialogResult.Secondary:
                // 不保存直接退出
                AppState.HasUnsavedChanges = false;
                Close();
                break;

            case ContentDialogResult.None:
                // 取消，维持窗口打开
                break;
        }
    }
}
