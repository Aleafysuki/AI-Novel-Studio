using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// 小说章节
/// </summary>
public partial class Chapter : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    public partial string Title { get; set; } = "新章节";

    [ObservableProperty]
    public partial string Content { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Order { get; set; }

    [ObservableProperty]
    public partial long WordCount { get; set; }

    [ObservableProperty]
    public partial ChapterStatus Status { get; set; } = ChapterStatus.Draft;

    [ObservableProperty]
    public partial List<string> CharacterIds { get; set; } = []; // 出場人物

    [ObservableProperty]
    public partial List<string> LocationIds { get; set; } = []; // 场景地点

    [ObservableProperty]
    public partial List<string> EventIds { get; set; } = []; // 关键事件

    [ObservableProperty]
    public partial string AiSummary { get; set; } = string.Empty; // AI 摘要 / 剧情概要

    [ObservableProperty]
    public partial List<string> DependencyIds { get; set; } = []; // 依赖的 EventIds

    // ===== V2 新增：AI 生成占比与元数据 =====

    /// <summary>
    /// AI 生成字数（用于计算 AI 生成占比）
    /// </summary>
    [ObservableProperty]
    public partial long AiGeneratedWords { get; set; }

    /// <summary>
    /// AI 生成占比 0-100（派生）
    /// </summary>
    public int AiRatio => WordCount <= 0 ? 0 : (int)Math.Min(100, AiGeneratedWords * 100 / WordCount);

    public string AiRatioLabel => $"AI {AiRatio}%";

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [ObservableProperty]
    public partial DateTime LastModifiedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 本章参与度覆盖（-1 表示沿用全局设置）
    /// </summary>
    [ObservableProperty]
    public partial double ParticipationOverride { get; set; } = -1;

    public bool HasParticipationOverride => ParticipationOverride >= 0;

    partial void OnWordCountChanged(long value)
    {
        OnPropertyChanged(nameof(AiRatio));
        OnPropertyChanged(nameof(AiRatioLabel));
    }

    partial void OnAiGeneratedWordsChanged(long value)
    {
        OnPropertyChanged(nameof(AiRatio));
        OnPropertyChanged(nameof(AiRatioLabel));
    }

    partial void OnParticipationOverrideChanged(double value)
        => OnPropertyChanged(nameof(HasParticipationOverride));

    // Diff 相关：当前章节是否处于 Diff 对比状态
    [ObservableProperty]
    public partial bool IsInDiffMode { get; set; }

    [ObservableProperty]
    public partial string DiffProposedContent { get; set; } = string.Empty; // AI 建议的修改后内容

    [ObservableProperty]
    public partial int DiffStartPosition { get; set; } // Diff 起始位置

    [ObservableProperty]
    public partial int DiffEndPosition { get; set; } // Diff 结束位置
}

public enum ChapterStatus
{
    Draft,      // 草稿
    Writing,    // 写作中
    Review,     // 待审核
    Completed,  // 已完成
    Published   // 已发布
}
