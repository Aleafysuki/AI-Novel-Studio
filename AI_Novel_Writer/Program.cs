using System;
using Microsoft.UI.Xaml;
using WinRT;

#if UNPACKAGED
using Microsoft.Windows.ApplicationModel.DynamicDependency;
#endif

namespace AINovelWriter;

/// <summary>
/// 显式应用入口点。
///
/// WinUI 3 + Windows App SDK 支持两种 unpackaged 部署模式：
///
/// 1. 自包含模式（WindowsAppSDKSelfContained=true）：
///    - 运行时 DLL 拷贝到 exe 同目录
///    - Foundation 包在构建时自动注入 [ModuleInitializer]，通过 UndockedRegFreeWinRT
///      机制设置 MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY 并强制加载
///      WindowsAppRuntime.dll（COM 免注册激活）
///    - 此路径不需要调用 Bootstrap.Initialize()——后者是为"框架依赖"模式设计，
///      会尝试注册系统级的框架包依赖导致 "无法解析包依赖项条件" 错误
///
/// 2. 框架依赖模式（WindowsAppSDKSelfContained!=true / 无 WindowsPackageType）：
///    - 需要调用 Bootstrap.Initialize(0) 注册对已安装的
///      Microsoft.WindowsAppRuntime 框架包的动态依赖
///
/// 这里运行时检测 MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY 环境变量——该变量
/// 由 [ModuleInitializer] 在 Main() 入口之前设置，用于区分上述两种模式。
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if UNPACKAGED
        if (!IsSelfContained())
        {
            // 框架依赖模式：注册对系统已安装的 WinAppSDK 框架包的动态依赖
            try
            {
                Bootstrap.Initialize(0);
                StartupLogger.Log("Bootstrap", "Bootstrap.Initialize(0) OK (framework-dependent)");
            }
            catch (Exception ex)
            {
                StartupLogger.LogException("Bootstrap.Failed", ex);
                return;
            }
        }
        else
        {
            // 自包含模式：[ModuleInitializer] 已通过 UndockedRegFreeWinRT 机制加载运行时。
            // 不调用 Bootstrap.Initialize——该 API 在自包含模式下会因找不到系统框架包而
            // 抛出 "无法解析包依赖项条件" 的 COMException。
            StartupLogger.Log("Bootstrap", "Self-contained mode: skipping Bootstrap.Initialize (handled by [ModuleInitializer] UndockedRegFreeWinRT)");
        }
#endif

        StartupLogger.LogPackageInfo();

        ComWrappersSupport.InitializeComWrappers();
        Application.Start((p) =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    /// <summary>
    /// 通过检查 MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY 环境变量判断是否处于自包含模式。
    /// 该变量由 Foundation 包自动注入的 [ModuleInitializer]
    /// （UndockedRegFreeWinRT-AutoInitializer.cs）在进程启动时设置。
    /// 如果已设置，说明运行时已通过 UndockedRegFreeWinRT 机制加载，无需调用 Bootstrap。
    /// </summary>
    private static bool IsSelfContained()
    {
        try
        {
            var baseDir = Environment.GetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY");
            return !string.IsNullOrEmpty(baseDir);
        }
        catch
        {
            return false;
        }
    }
}
