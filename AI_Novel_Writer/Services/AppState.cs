using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using AINovelWriter.Models;
using AINovelWriter.Services;

namespace AINovelWriter.Services;

/// <summary>
/// 全局应用状态与导航中枢
/// 在多个页面（首页 / 创建向导 / 大纲 / 编辑器）之间共享当前项目并提供导航
/// </summary>
public static class AppState
{
    /// <summary>
    /// 根导航 Frame（由 MainWindow 注入）
    /// </summary>
    public static Frame? RootFrame { get; set; }

    /// <summary>
    /// 当前打开的项目
    /// </summary>
    public static NovelProject? CurrentProject { get; set; }

    /// <summary>
    /// 共享的服务实例
    /// </summary>
    public static ProjectService ProjectService { get; } = new();
    public static AIService AIService { get; } = new(new MaterialService());
    public static DiffService DiffService { get; } = new();
    public static WebSearchService WebSearchService { get; } = new();
    public static TextAnalysisService TextAnalysisService { get; } = new();
    public static AutoSaveService AutoSaveService { get; } = new();

    /// <summary>
    /// 全局应用设置（已随应用启动加载）
    /// </summary>
    public static AppSettings Settings => SettingsService.Current;

    /// <summary>
    /// 当前打开项目的文件路径（用于"保存"时直接覆盖；为空表示尚未保存过）
    /// </summary>
    public static string? CurrentProjectPath { get; set; }

    /// <summary>
    /// 是否有未保存的变更（由各 ViewModel 在编辑时设为 true，保存后设为 false）
    /// MainWindow 在关闭时检查此标记
    /// </summary>
    public static bool HasUnsavedChanges { get; set; }

    public static void Navigate(Type pageType, object? parameter = null)
    {
        RootFrame?.Navigate(pageType, parameter, new DrillInNavigationTransitionInfo());
    }

    public static void GoBack()
    {
        if (RootFrame?.CanGoBack == true)
            RootFrame.GoBack();
    }
}
