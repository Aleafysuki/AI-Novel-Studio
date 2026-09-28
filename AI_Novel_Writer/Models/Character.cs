using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// 角色档案（含原作设定 + 创作补充）
/// </summary>
public partial class Character : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    public partial string Name { get; set; } = "未命名角色";

    [ObservableProperty]
    public partial string Alias { get; set; } = string.Empty; // 别名/外号

    [ObservableProperty]
    public partial string Role { get; set; } = string.Empty; // 主角/配角/反派

    [ObservableProperty]
    public partial bool IsOriginalCharacter { get; set; } = true; // 是否为原作角色

    // 人物设定
    [ObservableProperty]
    public partial string PersonalityTags { get; set; } = string.Empty; // 性格标签（逗号分隔）

    // 自由填写的角色设定（可选）：口头禅 / 外貌 / 背景 / 能力 / 动机，或其他任何自定义内容
    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Catchphrase { get; set; } = string.Empty; // 口头禅（兼容旧数据，新编辑统一写入 Notes）

    [ObservableProperty]
    public partial string Appearance { get; set; } = string.Empty; // 外貌描述

    [ObservableProperty]
    public partial string Background { get; set; } = string.Empty; // 背景故事

    [ObservableProperty]
    public partial string Abilities { get; set; } = string.Empty; // 能力/技能

    [ObservableProperty]
    public partial string Motivation { get; set; } = string.Empty; // 动机

    // 关系图谱
    [ObservableProperty]
    public partial List<CharacterRelationship> Relationships { get; set; } = [];

    // 出场记录
    [ObservableProperty]
    public partial List<string> AppearanceChapterIds { get; set; } = [];

    // RAG 相关
    [ObservableProperty]
    public partial string SourceQuote { get; set; } = string.Empty; // 原作中的关键引用

    [ObservableProperty]
    public partial string SourceVolume { get; set; } = string.Empty; // 原作出处（卷/章）
}

/// <summary>
/// 角色关系
/// </summary>
public class CharacterRelationship
{
    public string TargetCharacterId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty; // 关系类型：朋友/敌人/恋人/师徒
    public string Description { get; set; } = string.Empty;
}
