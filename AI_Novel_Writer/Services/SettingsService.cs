using System.Text.Json;
using AINovelWriter.Models;
using Windows.Storage;

namespace AINovelWriter.Services;

/// <summary>
/// 全局设置持久化服务：将 AppSettings 以 JSON 形式存到应用本地目录
/// </summary>
public static class SettingsService
{
    private const string FileName = "appsettings.json";

    public static AppSettings Current { get; private set; } = new();

    public static void Load()
    {
        try
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = folder.TryGetItemAsync(FileName).AsTask().Result as StorageFile;
            if (file != null)
            {
                var json = FileIO.ReadTextAsync(file).AsTask().Result;
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    // 补齐可能缺失的命令模板
                    foreach (AIBlockKind kind in Enum.GetValues<AIBlockKind>())
                        if (!loaded.PromptTemplates.Any(t => t.Kind == kind))
                            loaded.PromptTemplates.Add(new PromptTemplateItem { Kind = kind, Template = AppSettings.DefaultTemplate(kind) });
                    Current = loaded;
                }
            }
        }
        catch
        {
            // 读取失败则使用内存中的默认设置
            Current = new AppSettings();
        }
    }

    public static void Save()
    {
        try
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = folder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting).AsTask().Result;
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            FileIO.WriteTextAsync(file, json).AsTask().Wait();
        }
        catch
        {
            // 保存失败静默忽略（原型阶段）
        }
    }
}
