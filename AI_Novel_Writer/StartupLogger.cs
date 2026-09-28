using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AINovelWriter;

/// <summary>
/// 极简启动日志：写入 %LocalAppData%\AINovelWriter\Logs\startup_*.log。
/// 用于在陌生机器上排查"启动即崩溃 / 双击无反应"问题。
/// 设计原则：所有写操作都包 try/catch，自身绝不抛异常、绝不影响启动流程。
/// </summary>
public static class StartupLogger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AINovelWriter", "Logs");

    private static readonly string LogPath = "";
    private static readonly object _gate = new();
    private static bool _ready;

    static StartupLogger()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            LogPath = Path.Combine(LogDir,
                $"startup_{DateTime.Now:yyyyMMdd_HHmmss}_{Environment.ProcessId}.log");
            _ready = true;
            TrimOldLogs();
            // 注意：静态构造函数里只做"安全"的事（不触碰任何 WinAppSDK 投影类型）。
            // Package.Current 的探测放到显式的 LogPackageInfo()，在 Bootstrap 初始化之后再调用，
            // 否则会在运行时注册前触发投影层的自动初始化静态构造函数并崩溃。
            WriteRaw(DumpEnvironment());
        }
        catch
        {
            _ready = false;
        }
    }

    /// <summary>
    /// 显式记录打包状态（IsPackaged）。必须在 Bootstrap.Initialize() 之后调用，
    /// 因为 Windows.ApplicationModel.Package.Current 属于 WinAppSDK 投影类型，
    /// 过早访问会触发自动初始化失败。
    /// </summary>
    public static void LogPackageInfo()
    {
        if (!_ready) return;
        try
        {
            var fam = global::Windows.ApplicationModel.Package.Current.Id.FamilyName;
            Log("Env", $"IsPackaged : true (Family={fam})");
        }
        catch (Exception ex)
        {
            Log("Env", $"IsPackaged : false (Package.Current threw {ex.GetType().Name})");
        }
    }

    public static string CurrentLogPath => LogPath;
    public static bool IsReady => _ready;

    /// <summary>记录一条带时间戳和标签的日志。</summary>
    public static void Log(string tag, string message)
    {
        if (!_ready) return;
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {message}";
            WriteRaw(line);
        }
        catch { }
    }

    /// <summary>记录一条异常（含类型、消息、堆栈）。</summary>
    public static void LogException(string tag, Exception? ex)
    {
        if (ex == null) return;
        Log(tag, $"EXCEPTION {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
    }

    private static string DumpEnvironment()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== AINovelWriter startup log ===");
        sb.AppendLine($"UtcStart   : {DateTime.UtcNow:o}");
        sb.AppendLine($"LocalStart : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"ProcessId  : {Environment.ProcessId}");
        sb.AppendLine($"BaseDir    : {AppContext.BaseDirectory}");
        sb.AppendLine($"CommandLine: {Environment.CommandLine}");
        sb.AppendLine($"OSVersion  : {Environment.OSVersion}");
        sb.AppendLine($"OSDesc     : {RuntimeInformation.OSDescription}");
        sb.AppendLine($"ProcArch   : {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"Framework  : {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"RuntimeVer : {Environment.Version}");
        // 列出 exe 同目录里几个关键运行时文件是否存在，便于判断依赖是否齐全
        try
        {
            var dir = AppContext.BaseDirectory;
            string[] probe = {
                "Microsoft.WindowsAppRuntime.dll",
                "Microsoft.WinUI.dll",
                "Microsoft.WindowsAppRuntime.Bootstrap.dll",
                "Microsoft.WindowsAppRuntime.Bootstrap.Net.dll",
                "vcruntime140.dll",
                "Assets/AppIcon.ico"
            };
            sb.AppendLine("Runtime file presence (next to exe):");
            foreach (var p in probe)
            {
                var full = Path.Combine(dir, p);
                sb.AppendLine($"  {(File.Exists(full) ? "OK  " : "MISS")} {p}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Runtime file probe failed: {ex.Message}");
        }
        return sb.ToString();
    }

    private static void WriteRaw(string text)
    {
        if (!_ready) return;
        try
        {
            lock (_gate)
            {
                File.AppendAllText(LogPath, text + Environment.NewLine);
            }
        }
        catch { }
    }

    private static void TrimOldLogs()
    {
        try
        {
            var files = new DirectoryInfo(LogDir)
                .GetFiles("startup_*.log")
                .OrderBy(f => f.CreationTimeUtc)
                .ToList();
            // 仅保留最近 30 个，避免无限增长
            for (int i = 0; i < files.Count - 30; i++)
            {
                try { files[i].Delete(); } catch { }
            }
        }
        catch { }
    }
}
