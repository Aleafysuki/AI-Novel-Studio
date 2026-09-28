using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AINovelWriter.Models;

/// <summary>
/// 小说项目 - 核心数据模型
/// </summary>
public partial class NovelProject : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    public partial string Title { get; set; } = "新项目";

    [ObservableProperty]
    public partial string OriginalWork { get; set; } = string.Empty; // 原作名称

    [ObservableProperty]
    public partial string Synopsis { get; set; } = string.Empty; // 简介

    [ObservableProperty]
    public partial string Outline { get; set; } = string.Empty; // 大纲

    [ObservableProperty]
    public partial string WritingGoal { get; set; } = string.Empty; // 当前写作目标

    [ObservableProperty]
    public partial List<string> Tags { get; set; } = [];

    [ObservableProperty]
    public partial DateTime CreatedAt { get; set; } = DateTime.Now;

    [ObservableProperty]
    public partial DateTime LastModifiedAt { get; set; } = DateTime.Now;

    [ObservableProperty]
    public partial long TotalWords { get; set; }

    // ===== V2 新增：作品创建向导元数据 =====

    /// <summary>
    /// 小说类型（玄幻 / 都市 / 科幻 …）
    /// </summary>
    [ObservableProperty]
    public partial string Genre { get; set; } = "玄幻";

    /// <summary>
    /// 创作模式：原创 / 同人
    /// </summary>
    [ObservableProperty]
    public partial CreationMode Mode { get; set; } = CreationMode.Original;

    /// <summary>
    /// 目标平台（起点 / 番茄 / 晋江 …）
    /// </summary>
    [ObservableProperty]
    public partial string TargetPlatform { get; set; } = "起点中文网";

    /// <summary>
    /// AI 创作风格
    /// </summary>
    [ObservableProperty]
    public partial string AiStyle { get; set; } = "轻小说";

    public bool IsFanFiction => Mode == CreationMode.FanFiction;

    // ===== V2 核心配置对象 =====

    /// <summary>
    /// 全局 AI 参与度（核心卖点）
    /// </summary>
    public AIParticipation Participation { get; set; } = new();

    /// <summary>
    /// 可视化 Prompt 配置
    /// </summary>
    public PromptConfig Prompt { get; set; } = new();

    // 子集合
    public ObservableCollection<Character> Characters { get; set; } = new();
    public List<Location> Locations { get; set; } = [];
    public ObservableCollection<Chapter> Chapters { get; set; } = [];
    public List<NovelEvent> Events { get; set; } = [];
    public List<MaterialDocument> Materials { get; set; } = [];

    /// <summary>
    /// 世界观设定（独立管理）
    /// </summary>
    public ObservableCollection<WorldSetting> WorldSettings { get; set; } = [];

    /// <summary>
    /// 大纲树（卷 → 章 → 场景）
    /// </summary>
    public ObservableCollection<OutlineNode> Outlines { get; set; } = [];

    /// <summary>
    /// AI 操作历史
    /// </summary>
    public ObservableCollection<AIHistoryRecord> History { get; set; } = [];

    // 当前编辑状态
    [ObservableProperty]
    public partial string CurrentChapterId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentCharacterId { get; set; } = string.Empty;

    partial void OnModeChanged(CreationMode value) => OnPropertyChanged(nameof(IsFanFiction));
}

public enum CreationMode
{
    Original,    // 原创
    FanFiction   // 同人
}
