using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AINovelWriter.Models;

/// <summary>
/// 大纲节点 —— 支持"创意 → 小说大纲 → 章节大纲"的分层规划
/// V1.5 旗舰功能：让 AI 从一开始就"按规划写"
/// </summary>
public partial class OutlineNode : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    public partial OutlineLevel Level { get; set; } = OutlineLevel.Volume;

    [ObservableProperty]
    public partial string Title { get; set; } = "新节点";

    /// <summary>
    /// 该节点的剧情概要
    /// </summary>
    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 关键人物
    /// </summary>
    [ObservableProperty]
    public partial string KeyCharacters { get; set; } = string.Empty;

    /// <summary>
    /// 是否已由用户确认（确认后才能进入下一层生成）
    /// </summary>
    [ObservableProperty]
    public partial bool IsConfirmed { get; set; }

    /// <summary>
    /// 是否已生成正文
    /// </summary>
    [ObservableProperty]
    public partial bool HasContent { get; set; }

    /// <summary>
    /// 对应章节 Id（若已生成正文）
    /// </summary>
    [ObservableProperty]
    public partial string LinkedChapterId { get; set; } = string.Empty;

    /// <summary>
    /// 子节点（卷→章）
    /// </summary>
    public ObservableCollection<OutlineNode> Children { get; set; } = [];

    public string LevelLabel => Level switch
    {
        OutlineLevel.Volume => "卷",
        OutlineLevel.Chapter => "章",
        OutlineLevel.Scene => "场景",
        _ => ""
    };
}

public enum OutlineLevel
{
    Volume,   // 卷
    Chapter,  // 章
    Scene     // 场景
}
