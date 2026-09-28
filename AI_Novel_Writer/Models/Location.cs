using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// 地点/场景设定
/// </summary>
public partial class Location : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    public partial string Name { get; set; } = "未命名地点";

    [ObservableProperty]
    public partial string Category { get; set; } = string.Empty; // 城市/建筑/自然景观/室内

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Atmosphere { get; set; } = string.Empty; // 氛围描述

    [ObservableProperty]
    public partial bool IsOriginalLocation { get; set; } = true;

    [ObservableProperty]
    public partial string SourceVolume { get; set; } = string.Empty;
}
