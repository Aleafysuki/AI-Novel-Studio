using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// 可视化 Prompt 配置 —— 不用每次重新输入 Prompt
/// </summary>
public partial class PromptConfig : ObservableObject
{
    /// <summary>
    /// 当前写作风格
    /// </summary>
    [ObservableProperty]
    public partial string Style { get; set; } = "轻小说";

    public static List<string> StyleOptions { get; } =
        ["轻小说", "恋爱", "悬疑", "日常", "搞笑", "黑暗", "史诗", "武侠", "科幻"];

    // 供 x:Bind 通过实例路径访问的包装属性
    public List<string> Styles => StyleOptions;
    public List<string> Consistencies => ConsistencyOptions;
    public List<string> Freedoms => FreedomOptions;

    /// <summary>
    /// 人物一致性：高 / 中 / 低
    /// </summary>
    [ObservableProperty]
    public partial string CharacterConsistency { get; set; } = "高";

    public static List<string> ConsistencyOptions { get; } = ["高", "中", "低"];

    /// <summary>
    /// 创作自由度：严格遵守设定 / 适当发挥 / 自由发挥
    /// </summary>
    [ObservableProperty]
    public partial string CreativeFreedom { get; set; } = "适当发挥";

    public static List<string> FreedomOptions { get; } = ["严格遵守设定", "适当发挥", "自由发挥"];

    /// <summary>
    /// 目标平台（起点/番茄/晋江等）
    /// </summary>
    [ObservableProperty]
    public partial string TargetPlatform { get; set; } = "起点中文网";

    /// <summary>
    /// 自定义补充 Prompt
    /// </summary>
    [ObservableProperty]
    public partial string CustomInstruction { get; set; } = string.Empty;

    /// <summary>
    /// 组装成最终 System Prompt 预览
    /// </summary>
    public string BuildSystemPrompt(string worldContext, string characterContext)
    {
        return $"""
                [写作风格] {Style}
                [目标平台] {TargetPlatform}
                [人物一致性] {CharacterConsistency}
                [创作自由度] {CreativeFreedom}
                --- 世界观上下文 ---
                {worldContext}
                --- 人物设定 ---
                {characterContext}
                {(string.IsNullOrWhiteSpace(CustomInstruction) ? "" : $"--- 补充要求 ---\n{CustomInstruction}")}
                """;
    }
}
