using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// 级联修改冲突
/// </summary>
public partial class CascadeConflict : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// 触发修改的事件
    /// </summary>
    [ObservableProperty]
    public partial string SourceEventId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SourceEventTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SourceChapterId { get; set; } = string.Empty;

    /// <summary>
    /// 被影响的章节
    /// </summary>
    [ObservableProperty]
    public partial string AffectedChapterId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AffectedChapterTitle { get; set; } = string.Empty;

    /// <summary>
    /// 冲突描述
    /// </summary>
    [ObservableProperty]
    public partial string ConflictDescription { get; set; } = string.Empty;

    /// <summary>
    /// 原文本
    /// </summary>
    [ObservableProperty]
    public partial string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// 建议修改文本
    /// </summary>
    [ObservableProperty]
    public partial string SuggestedText { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    [ObservableProperty]
    public partial ConflictSeverity Severity { get; set; } = ConflictSeverity.Medium;

    /// <summary>
    /// 用户是否已处理
    /// </summary>
    [ObservableProperty]
    public partial bool IsResolved { get; set; }
}

public enum ConflictSeverity
{
    Low,     // 可忽略的类型不一致
    Medium,  // 需要确认的逻辑冲突
    High,    // 严重矛盾（角色状态不一致）
    Critical // 致命矛盾（死人复活等）
}
