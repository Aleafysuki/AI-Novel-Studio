using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// 关键情节事件（用于依赖追踪和级联修改）
/// </summary>
public partial class NovelEvent : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    public partial string Title { get; set; } = "未命名事件";

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ChapterId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Position { get; set; } // 在章节中的位置

    // 前驱事件（本事件依赖的前提）
    [ObservableProperty]
    public partial List<string> PrerequisiteEventIds { get; set; } = [];

    // 后继事件（依赖本事件的事件）
    [ObservableProperty]
    public partial List<string> DependentEventIds { get; set; } = [];

    // 涉及的人物
    [ObservableProperty]
    public partial List<string> InvolvedCharacterIds { get; set; } = [];

    // 事件性质
    [ObservableProperty]
    public partial EventNature Nature { get; set; } = EventNature.Neutral;
}

public enum EventNature
{
    Death,      // 死亡事件（级联影响最大）
    Injury,     // 受伤
    Betrayal,   // 背叛
    Discovery,  // 发现
    Meeting,    // 相遇
    Separation, // 分离
    Victory,    // 胜利
    Defeat,     // 失败
    Neutral     // 中性事件
}
