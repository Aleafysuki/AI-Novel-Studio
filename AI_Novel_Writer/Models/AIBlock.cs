using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// AI 生成块 —— 每一次 AI 生成都是一个独立的"生成块"
/// 用户可对每个块执行：插入正文 / 重新生成 / 继续 / 修改 Prompt
/// 这是 AI-generates / human-controls 范式的核心交互单元
/// </summary>
public partial class AIBlock : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// 生成类型（续写 / 扩写 / 重写 / 改对白 …）
    /// </summary>
    [ObservableProperty]
    public partial AIBlockKind Kind { get; set; } = AIBlockKind.Continue;

    /// <summary>
    /// 触发该生成块的用户指令 / 快捷命令
    /// </summary>
    [ObservableProperty]
    public partial string Command { get; set; } = string.Empty;

    /// <summary>
    /// 该生成块使用的选中原文（若有）
    /// </summary>
    [ObservableProperty]
    public partial string SourceText { get; set; } = string.Empty;

    /// <summary>
    /// AI 生成的内容
    /// </summary>
    [ObservableProperty]
    public partial string GeneratedText { get; set; } = string.Empty;

    /// <summary>
    /// AI 对本次修改的说明（可选，用户可要求"解释修改"）
    /// </summary>
    [ObservableProperty]
    public partial string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 该块生成时的参与度快照
    /// </summary>
    [ObservableProperty]
    public partial double ParticipationSnapshot { get; set; } = 30;

    /// <summary>
    /// 状态：待处理 / 已插入 / 已丢弃
    /// </summary>
    [ObservableProperty]
    public partial AIBlockStatus Status { get; set; } = AIBlockStatus.Pending;

    /// <summary>
    /// 该块将要插入的正文位置（光标偏移）
    /// </summary>
    [ObservableProperty]
    public partial int InsertPosition { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    [ObservableProperty]
    public partial DateTime CreatedAt { get; set; } = DateTime.Now;

    public string KindLabel => Kind switch
    {
        AIBlockKind.Continue => "续写",
        AIBlockKind.Expand => "扩写",
        AIBlockKind.Abbreviate => "缩写",
        AIBlockKind.Rewrite => "重写",
        AIBlockKind.ChangeDialogue => "改对白",
        AIBlockKind.ChangeDescription => "改描写",
        AIBlockKind.AddDetail => "加细节",
        AIBlockKind.AddConflict => "加冲突",
        AIBlockKind.AddPsychology => "加心理",
        AIBlockKind.AddForeshadow => "加伏笔",
        AIBlockKind.AdjustPace => "调节奏",
        AIBlockKind.ToFirstPerson => "转第一人称",
        AIBlockKind.ToThirdPerson => "转第三人称",
        _ => "生成"
    };

    public string StatusLabel => Status switch
    {
        AIBlockStatus.Pending => "待处理",
        AIBlockStatus.Inserted => "已插入",
        AIBlockStatus.Discarded => "已丢弃",
        _ => ""
    };
}

/// <summary>
/// AI 生成块类型 —— 对应选中文本后的 20+ 快捷命令
/// </summary>
public enum AIBlockKind
{
    Continue,          // 续写
    Expand,            // 扩写
    Abbreviate,        // 缩写
    Rewrite,           // 重写
    ChangeDialogue,    // 改对白
    ChangeDescription, // 改描写
    AddDetail,         // 加细节
    AddConflict,       // 加冲突
    AddPsychology,     // 加心理描写
    AddForeshadow,     // 加伏笔
    AdjustPace,        // 调整节奏
    ToFirstPerson,     // 转第一人称
    ToThirdPerson      // 转第三人称
}

public enum AIBlockStatus
{
    Pending,    // 待处理
    Inserted,   // 已插入正文
    Discarded   // 已丢弃
}
