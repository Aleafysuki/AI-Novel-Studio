using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// 世界观设定（独立管理，AI 自动引用以保持一致性）
/// </summary>
public partial class WorldSetting : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    public partial WorldSettingType Type { get; set; } = WorldSettingType.Rule;

    [ObservableProperty]
    public partial string Title { get; set; } = "新设定";

    [ObservableProperty]
    public partial string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否锁定（锁定后 AI 严格遵守，不得违背）
    /// </summary>
    [ObservableProperty]
    public partial bool IsLocked { get; set; } = true;

    public string TypeLabel => Type switch
    {
        WorldSettingType.Rule => "世界规则",
        WorldSettingType.Faction => "势力",
        WorldSettingType.Place => "地点",
        WorldSettingType.Timeline => "时间线",
        WorldSettingType.Special => "特殊设定",
        _ => "其他"
    };
}

public enum WorldSettingType
{
    Rule,      // 世界规则
    Faction,   // 势力
    Place,     // 地点
    Timeline,  // 时间线
    Special    // 特殊设定
}
