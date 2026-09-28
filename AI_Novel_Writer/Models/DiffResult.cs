using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// Diff 差异结果
/// </summary>
public class DiffResult
{
    public string ChapterId { get; set; } = string.Empty;
    public List<DiffBlock> Blocks { get; set; } = [];
    public string OperationType { get; set; } = string.Empty; // "polish", "rewrite", "cascade_fix"
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 单个差异块
/// </summary>
public partial class DiffBlock : ObservableObject
{
    [ObservableProperty]
    public partial DiffType Type { get; set; }

    [ObservableProperty]
    public partial string OriginalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProposedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int StartLine { get; set; }

    [ObservableProperty]
    public partial int EndLine { get; set; }

    /// <summary>
    /// 用户是否已接受此修改
    /// </summary>
    [ObservableProperty]
    public partial DiffDecision UserDecision { get; set; } = DiffDecision.Pending;

    /// <summary>
    /// 修改原因/说明
    /// </summary>
    [ObservableProperty]
    public partial string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 是否为级联修改触发的
    /// </summary>
    [ObservableProperty]
    public partial bool IsCascadeTriggered { get; set; }
}

public enum DiffType
{
    Equal,
    Insert,
    Delete,
    Replace
}

public enum DiffDecision
{
    Pending,
    Accepted,
    Rejected
}
