using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// AI 参与度配置 —— 核心卖点
/// 控制 AI 在整个创作流程中的"主动权"，而非仅仅字数
/// 支持全局默认 + 章节级/场景级/选区级临时覆盖
/// </summary>
public partial class AIParticipation : ObservableObject
{
    /// <summary>
    /// 参与度百分比 0-100
    /// </summary>
    [ObservableProperty]
    public partial double Level { get; set; } = 30;

    /// <summary>
    /// 当前模式名称（根据 Level 派生）
    /// </summary>
    public string ModeName => Level switch
    {
        <= 5 => "纯人工模式",
        <= 35 => "轻度辅助",
        <= 65 => "协同创作",
        <= 90 => "AI 主写",
        _ => "全自动创作"
    };

    /// <summary>
    /// 当前模式说明
    /// </summary>
    public string ModeDescription => Level switch
    {
        <= 5 => "AI 不会主动生成正文，仅提供拼写检查、润色建议、人设与剧情冲突提醒。适合传统作者。",
        <= 35 => "AI 可续写几十字、提供对白与剧情方向建议。用户仍负责正文主体。",
        <= 65 => "AI 与用户共同参与，一人一句式创作。AI 主动补充描写、对白与设定。",
        <= 90 => "用户提供本章目标与剧情概要，AI 自动生成完整正文，用户负责修改与节奏把控。适合网文日更作者。",
        _ => "用户仅提供世界观、人设与大纲，AI 自动完成分章、正文、对话与描写，用户最后审核。"
    };

    // ===== 能力矩阵（根据参与度自动派生） =====
    public bool CanAutoContinue => Level > 20;         // 自动续写
    public bool CanAutoExpand => Level > 15;           // 自动扩写
    public bool CanAutoBuildSetting => Level > 45;     // 自动补设定
    public bool CanAutoDialogue => Level > 20;         // 主动写对白
    public bool CanAdvancePlot => Level > 45;          // 主动推进剧情
    public bool CanCompleteChapter => Level > 70;      // 自动完成章节

    /// <summary>
    /// 覆盖来源标记（全局 / 章节 / 场景 / 选区）
    /// </summary>
    [ObservableProperty]
    public partial ParticipationScope Scope { get; set; } = ParticipationScope.Global;

    /// <summary>
    /// 手动触发派生属性刷新
    /// </summary>
    public void NotifyDerived()
    {
        OnPropertyChanged(nameof(ModeName));
        OnPropertyChanged(nameof(ModeDescription));
        OnPropertyChanged(nameof(CanAutoContinue));
        OnPropertyChanged(nameof(CanAutoExpand));
        OnPropertyChanged(nameof(CanAutoBuildSetting));
        OnPropertyChanged(nameof(CanAutoDialogue));
        OnPropertyChanged(nameof(CanAdvancePlot));
        OnPropertyChanged(nameof(CanCompleteChapter));
    }

    partial void OnLevelChanged(double value) => NotifyDerived();
}

public enum ParticipationScope
{
    Global,   // 全书默认
    Chapter,  // 章节临时覆盖
    Scene,    // 场景临时覆盖
    Selection // 选区临时覆盖
}

/// <summary>
/// 高级多维度参与控制（可扩展）
/// </summary>
public partial class AIDimensions : ObservableObject
{
    [ObservableProperty] public partial double PlotCreativity { get; set; } = 50;   // 剧情创造力
    [ObservableProperty] public partial double CharacterAgency { get; set; } = 60;  // 角色主动性
    [ObservableProperty] public partial double DescriptionRichness { get; set; } = 70; // 描写丰富度
    [ObservableProperty] public partial double DialogueRatio { get; set; } = 40;    // 对白占比
    [ObservableProperty] public partial double StyleStability { get; set; } = 90;   // 文风稳定性
}
