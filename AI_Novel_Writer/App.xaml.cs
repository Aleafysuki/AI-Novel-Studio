using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using AINovelWriter.Services;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AINovelWriter;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// The main application window. Use <c>App.Window</c> from any class that needs
    /// the window reference (for dialogs, pickers, interop, etc.).
    /// </summary>
    public static Window Window { get; private set; } = null!;

    /// <summary>
    /// The UI thread dispatcher. Use <c>App.DispatcherQueue</c> to marshal calls
    /// to the UI thread. Fully qualified to avoid CS0104 ambiguity with
    /// <see cref="Windows.System.DispatcherQueue"/>.
    /// </summary>
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    /// <summary>
    /// The native window handle (HWND). Use for file pickers,
    /// <c>DataTransferManager</c>, and any WinRT interop that requires
    /// <c>InitializeWithWindow</c>.
    /// </summary>
    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        StartupLogger.Log("App", "App() ctor entered");
        try
        {
            InitializeComponent();
            StartupLogger.Log("App", "InitializeComponent() succeeded");
        }
        catch (Exception ex)
        {
            StartupLogger.LogException("App.Init", ex);
            throw;
        }

        // 捕获各路未处理异常并落盘（先记录再按默认行为退出，保证日志不丢）
        this.UnhandledException += (s, e) => StartupLogger.LogException("WinUI.Unhandled", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            StartupLogger.Log("AppDomain.Unhandled", $"isTerminating={e.IsTerminating}, ex={(e.ExceptionObject as Exception)?.ToString() ?? e.ExceptionObject?.ToString()}");
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            StartupLogger.LogException("Task.Unobserved", e.Exception);
            e.SetObserved();
        };

        StartupLogger.Log("App", "App() ctor done");
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        StartupLogger.Log("Launch", "OnLaunched entered");
        try
        {
            SettingsService.Load();
            StartupLogger.Log("Launch", "SettingsService.Load() done");
            Window = new MainWindow();
            StartupLogger.Log("Launch", "MainWindow constructed");
            ApplyTheme();
            StartupLogger.Log("Launch", "ApplyTheme() done");
            DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            Window.Activate();
            StartupLogger.Log("Launch", "Window.Activated -> startup complete");
        }
        catch (Exception ex)
        {
            StartupLogger.LogException("Launch.Failed", ex);
            throw;
        }
    }

    /// <summary>
    /// 应用外观主题：跟随系统 / 浅色 / 深色。读取 AppState.Settings.Theme。
    /// 通过根 FrameworkElement.RequestedTheme（ElementTheme）实现运行时切换，
    /// 其中 Default 即跟随系统，Light/Dark 为强制覆盖。
    /// </summary>
    public static void ApplyTheme()
    {
        ElementTheme theme = AppState.Settings.Theme switch
        {
            "深色" => ElementTheme.Dark,
            "浅色" => ElementTheme.Light,
            _ => ElementTheme.Default
        };
        if (Window.Content is FrameworkElement root)
            root.RequestedTheme = theme;
    }
}
