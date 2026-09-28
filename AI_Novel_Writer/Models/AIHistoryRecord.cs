using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// AI 操作历史记录 —— 支持展开 / 恢复 / 重新生成 / 删除
/// 让用户可以随时回退到任意一次 AI 生成前的状态
/// </summary>
public partial class AIHistoryRecord : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// 操作发生的章节 Id
    /// </summary>
    [ObservableProperty]
    public partial string ChapterId { get; set; } = string.Empty;

    /// <summary>
    /// 操作标题（如："续写 · 林逸出关"）
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型标签
    /// </summary>
    [ObservableProperty]
    public partial string ActionLabel { get; set; } = "AI 生成";

    /// <summary>
    /// 操作前的正文快照（用于恢复）
    /// </summary>
    [ObservableProperty]
    public partial string BeforeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 操作后的正文快照
    /// </summary>
    [ObservableProperty]
    public partial string AfterSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 使用的指令（可用于"重新生成"）
    /// </summary>
    [ObservableProperty]
    public partial string Command { get; set; } = string.Empty;

    /// <summary>
    /// 本次操作影响的字数
    /// </summary>
    [ObservableProperty]
    public partial int AffectedWords { get; set; }

    /// <summary>
    /// 是否为当前生效状态
    /// </summary>
    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    /// <summary>
    /// UI 是否展开显示详情
    /// </summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial DateTime CreatedAt { get; set; } = DateTime.Now;

    public string TimeLabel => CreatedAt.ToString("HH:mm:ss");
}
