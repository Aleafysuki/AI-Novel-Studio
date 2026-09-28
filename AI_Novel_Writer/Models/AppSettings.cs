using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AINovelWriter.Models;

/// <summary>
/// 单个命令的可定制 Prompt 模板
/// </summary>
public partial class PromptTemplateItem : ObservableObject
{
    [ObservableProperty]
    public partial AIBlockKind Kind { get; set; } = AIBlockKind.Continue;

    public string KindLabel => new AIBlock { Kind = Kind }.KindLabel;

    [ObservableProperty]
    public partial string Template { get; set; } = "";
}

/// <summary>
/// 全局应用设置（持久化到本地 JSON）
/// 包含：AI API 配置、自定义写作风格、各命令的可定制 Prompt 模板
/// </summary>
public partial class AppSettings : ObservableObject
{
    /// <summary>内置默认写作风格</summary>
    public static List<string> DefaultStyles { get; } =
        ["轻小说", "恋爱", "悬疑", "日常", "搞笑", "黑暗", "史诗", "武侠", "科幻"];

    /// <summary>可选的 API 提供方（实例属性，便于 x:Bind 通过 AppState.Settings 访问）</summary>
    public List<string> ApiProviders { get; } =
        ["本地模拟（无需密钥）", "OpenAI", "DeepSeek", "通义千问", "自定义"];

    [ObservableProperty]
    public partial string ApiProvider { get; set; } = "本地模拟（无需密钥）";

    [ObservableProperty]
    public partial string ApiKey { get; set; } = "";

    [ObservableProperty]
    public partial string ApiBaseUrl { get; set; } = "";

    [ObservableProperty]
    public partial string ApiModel { get; set; } = "";

    /// <summary>主题：跟随系统 / 浅色 / 深色</summary>
    public List<string> ThemeOptions { get; } = ["跟随系统", "浅色", "深色"];

    [ObservableProperty]
    public partial string Theme { get; set; } = "跟随系统";

    /// <summary>用户自定义的写作风格（与默认风格合并后在下拉框中显示）</summary>
    public ObservableCollection<string> CustomStyles { get; } = new();

    /// <summary>用户预设：命名风格 + 详细内容，可在设置中填写，合并进写作风格下拉并作为 Prompt 上下文</summary>
    public ObservableCollection<UserPreset> UserPresets { get; } = new();

    /// <summary>各 AI 命令的可定制 Prompt 模板</summary>
    public ObservableCollection<PromptTemplateItem> PromptTemplates { get; } = new();

    public AppSettings()
    {
        foreach (AIBlockKind kind in Enum.GetValues<AIBlockKind>())
            PromptTemplates.Add(new PromptTemplateItem { Kind = kind, Template = DefaultTemplate(kind) });
    }

    /// <summary>合并默认风格、自定义风格与用户预设名称</summary>
    public List<string> AllStyles()
        => DefaultStyles
            .Concat(CustomStyles)
            .Concat(UserPresets.Select(p => p.Name))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

    /// <summary>获取与某写作风格同名的用户预设内容（用于拼接到 AI Prompt）</summary>
    public string GetPresetContent(string styleName)
    {
        var preset = UserPresets.FirstOrDefault(p => p.Name == styleName);
        return preset != null && !string.IsNullOrWhiteSpace(preset.Content) ? preset.Content : "";
    }

    /// <summary>获取某命令的 Prompt 模板（为空则使用内置默认）</summary>
    public string GetTemplate(AIBlockKind kind)
    {
        var item = PromptTemplates.FirstOrDefault(t => t.Kind == kind);
        return item != null && !string.IsNullOrWhiteSpace(item.Template) ? item.Template : DefaultTemplate(kind);
    }

    public static string DefaultTemplate(AIBlockKind kind) => kind switch
    {
        AIBlockKind.Continue => "根据当前上下文自然延续剧情，保持人物性格与文风一致，不要重复已有内容。",
        AIBlockKind.Rewrite => "重写下列片段，提升文采与节奏，不改变核心信息与人设。",
        AIBlockKind.Expand => "在原文基础上扩写，补充感官细节、动作与心理活动，使画面更饱满。",
        AIBlockKind.Abbreviate => "精简下列片段，保留关键信息，使行文更紧凑有力。",
        AIBlockKind.ChangeDialogue => "改写人物对白，使其更符合角色性格与当前情境。",
        AIBlockKind.ChangeDescription => "重写环境/场景描写，增强画面感与氛围。",
        AIBlockKind.AddDetail => "在原文后补充细节描写。",
        AIBlockKind.AddConflict => "在原文后加入冲突或突发事件，提升张力与悬念。",
        AIBlockKind.AddPsychology => "补充人物心理活动与内心独白。",
        AIBlockKind.AddForeshadow => "埋设伏笔，为后续剧情做铺垫。",
        AIBlockKind.AdjustPace => "调整叙事节奏，用短句加快场面推进。",
        AIBlockKind.ToFirstPerson => "将叙述转换为第一人称视角，保持时态与语气一致。",
        AIBlockKind.ToThirdPerson => "将叙述转换为第三人称视角。",
        _ => "按指令处理下列文本。"
    };
}

/// <summary>
/// 用户预设（命名风格 + 详细内容），可在应用设置中自由增删，作为写作风格的扩展与 Prompt 上下文
/// </summary>
public partial class UserPreset : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Content { get; set; } = "";

    public string Header => string.IsNullOrWhiteSpace(Name) ? "(未命名预设)" : Name;
}
