using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovelWriter.Models;

/// <summary>
/// 导入的源材料文档
/// </summary>
public partial class MaterialDocument : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MaterialType Type { get; set; } = MaterialType.TXT;

    [ObservableProperty]
    public partial long FileSize { get; set; }

    [ObservableProperty]
    public partial DateTime ImportedAt { get; set; } = DateTime.Now;

    [ObservableProperty]
    public partial ImportStatus Status { get; set; } = ImportStatus.Pending;

    [ObservableProperty]
    public partial int TotalChunks { get; set; }

    [ObservableProperty]
    public partial int ProcessedChunks { get; set; }

    /// <summary>
    /// 提取的实体列表
    /// </summary>
    [ObservableProperty]
    public partial List<ExtractedEntity> ExtractedEntities { get; set; } = [];

    /// <summary>
    /// 文档摘要
    /// </summary>
    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;
}

/// <summary>
/// 提取的实体
/// </summary>
public class ExtractedEntity
{
    public string Name { get; set; } = string.Empty;
    public EntityType Type { get; set; }
    public string Context { get; set; } = string.Empty; // 出现上下文
    public int OccurrenceCount { get; set; }
    public List<string> Appearances { get; set; } = []; // 出现位置摘要
}

public enum EntityType
{
    Person,      // 人名
    Location,    // 地名
    Organization,// 组织/门派
    Item,        // 物品/法宝
    Skill,       // 技能/功法
    Event,       // 事件
    Concept      // 专有概念
}

public enum MaterialType
{
    TXT,
    EPUB,
    PDF
}

public enum ImportStatus
{
    Pending,      // 待处理
    Parsing,      // 解析中
    Chunking,     // 分块中
    Extracting,   // 实体提取中
    Indexing,     // 向量化索引中
    Completed,    // 完成
    Failed        // 失败
}
