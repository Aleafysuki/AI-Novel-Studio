using System.Text.Json;
using AINovelWriter.Models;

namespace AINovelWriter.Services;

/// <summary>
/// 自动暂存服务 —— 使用 System.Threading.Timer 定时将当前项目保存到本地备份文件，
/// 防止突发进程终止导致数据丢失。
/// 每 2 分钟自动保存一次（仅在项目有变更时）。
/// App 启动时检测备份文件，询问用户是否恢复。
/// </summary>
public class AutoSaveService
{
    private Timer? _timer;
    private NovelProject? _currentProject;
    private readonly string _backupDir;
    private readonly object _lock = new();
    private bool _saving;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
        IncludeFields = true
    };

    /// <summary>上次暂存的内容快照，用于比对是否有变更</summary>
    private string? _lastSavedSnapshot;
    private bool _hasChangesSinceLastSave;

    public AutoSaveService()
    {
        _backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AINovelWriter", "AutoSave");

        Directory.CreateDirectory(_backupDir);
    }

    /// <summary>
    /// 启动自动暂存（绑定到当前项目）
    /// </summary>
    public void Start(NovelProject project)
    {
        _currentProject = project;
        _hasChangesSinceLastSave = false;
        _lastSavedSnapshot = TakeSnapshot(project);
        _timer = new Timer(OnTimerTick, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        System.Diagnostics.Debug.WriteLine("[AutoSave] 已启动，每 2 分钟自动暂存");
    }

    /// <summary>
    /// 停止自动暂存
    /// </summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        System.Diagnostics.Debug.WriteLine("[AutoSave] 已停止");
    }

    /// <summary>
    /// 手动标记有未保存的变更
    /// </summary>
    public void MarkChanged()
    {
        _hasChangesSinceLastSave = true;
    }

    /// <summary>
    /// 立即执行一次暂存
    /// </summary>
    public Task SaveNowAsync()
    {
        return SaveBackupAsync(_currentProject);
    }

    /// <summary>
    /// 在保存到正式文件后调用，重置变更标记
    /// </summary>
    public void ResetAfterSave()
    {
        if (_currentProject != null)
            _lastSavedSnapshot = TakeSnapshot(_currentProject);
        _hasChangesSinceLastSave = false;
    }

    /// <summary>
    /// 检查是否有自动暂存的备份可恢复
    /// </summary>
    public static bool HasBackup(string projectId)
    {
        var path = GetBackupPath(projectId);
        return File.Exists(path);
    }

    /// <summary>
    /// 从备份加载项目
    /// </summary>
    public static async Task<NovelProject?> LoadBackupAsync(string projectId)
    {
        var path = GetBackupPath(projectId);
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<NovelProject>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoSave] 恢复备份失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取所有可恢复的备份项目 ID 列表
    /// </summary>
    public static string[] GetBackupProjectIds()
    {
        var dir = GetBackupDir();
        if (!Directory.Exists(dir)) return [];

        return Directory.GetFiles(dir, "*.autosave")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToArray();
    }

    /// <summary>
    /// 删除备份文件
    /// </summary>
    public static void DeleteBackup(string projectId)
    {
        var path = GetBackupPath(projectId);
        if (File.Exists(path))
        {
            try { File.Delete(path); }
            catch { /* 忽略删除失败 */ }
        }
    }

    private void OnTimerTick(object? state)
    {
        if (_saving) return;
        _saving = true;
        try
        {
            SaveBackupAsync(_currentProject).GetAwaiter().GetResult();
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task SaveBackupAsync(NovelProject? project)
    {
        if (project == null) return;

        // 检查是否有实际变更
        var currentSnapshot = TakeSnapshot(project);
        if (currentSnapshot == _lastSavedSnapshot && !_hasChangesSinceLastSave)
            return; // 无变更，跳过

        try
        {
            var path = GetBackupPath(project.Id);
            var json = JsonSerializer.Serialize(project, JsonOptions);
            await File.WriteAllTextAsync(path, json);

            lock (_lock)
            {
                _lastSavedSnapshot = currentSnapshot;
                _hasChangesSinceLastSave = false;
            }
            System.Diagnostics.Debug.WriteLine($"[AutoSave] 已暂存 ({project.Title})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoSave] 暂存失败: {ex.Message}");
        }
    }

    private static string GetBackupDir()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AINovelWriter", "AutoSave");
    }

    private static string GetBackupPath(string projectId)
    {
        return Path.Combine(GetBackupDir(), $"{projectId}.autosave");
    }

    /// <summary>
    /// 生成项目的快照字符串用于比对变更
    /// </summary>
    private static string? TakeSnapshot(NovelProject project)
    {
        try
        {
            return JsonSerializer.Serialize(new
            {
                project.Title,
                project.LastModifiedAt,
                Chapters = project.Chapters.Select(c => new { c.Id, c.Content, c.WordCount, c.LastModifiedAt }),
                Characters = project.Characters.Select(ch => new { ch.Id, ch.Name, ch.Notes }),
                project.TotalWords
            }, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
