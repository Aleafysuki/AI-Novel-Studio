using AINovelWriter.Models;

namespace AINovelWriter.Services;

/// <summary>
/// 资料库服务：文档导入、解析、分块、实体提取、向量化存储
/// 原型阶段使用模拟数据
/// </summary>
public class MaterialService
{
    private readonly List<MaterialDocument> _documents = [];

    /// <summary>
    /// 模拟导入文档
    /// </summary>
    public async Task<MaterialDocument> ImportDocumentAsync(string filePath)
    {
        var doc = new MaterialDocument
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            Type = DetectMaterialType(filePath),
            Status = ImportStatus.Parsing
        };

        _documents.Add(doc);

        // 模拟解析过程
        await SimulateProcessing(doc);

        return doc;
    }

    private async Task SimulateProcessing(MaterialDocument doc)
    {
        // 1. 解析文本
        doc.Status = ImportStatus.Parsing;
        await Task.Delay(500);

        // 2. 分块
        doc.Status = ImportStatus.Chunking;
        doc.TotalChunks = new Random().Next(50, 200);
        await Task.Delay(800);

        // 3. 实体提取
        doc.Status = ImportStatus.Extracting;
        doc.ExtractedEntities = GenerateMockEntities();
        await Task.Delay(600);

        // 4. 向量索引
        doc.Status = ImportStatus.Indexing;
        doc.ProcessedChunks = doc.TotalChunks;
        await Task.Delay(1000);

        doc.Status = ImportStatus.Completed;
        doc.Summary = $"已导入 {doc.FileName}，共 {doc.TotalChunks} 个文本块，提取 {doc.ExtractedEntities.Count} 个实体。";
    }

    private List<ExtractedEntity> GenerateMockEntities()
    {
        var names = new[] { "林逸", "苏婉清", "陈枫", "云梦泽", "青云宗", "天剑门",
                           "玄铁重剑", "九转金身决", "万妖山脉", "帝都" };
        var types = new[] { EntityType.Person, EntityType.Location, EntityType.Organization,
                           EntityType.Item, EntityType.Skill, EntityType.Concept };
        var random = new Random();

        return Enumerable.Range(0, random.Next(10, 30)).Select(i => new ExtractedEntity
        {
            Name = names[random.Next(names.Length)],
            Type = types[random.Next(types.Length)],
            Context = $"在第{i + 1}章首次出现",
            OccurrenceCount = random.Next(1, 50),
            Appearances = [$"第{i + 1}章", $"第{i * 2 + 1}章"]
        }).ToList();
    }

    private MaterialType DetectMaterialType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".txt" => MaterialType.TXT,
            ".epub" => MaterialType.EPUB,
            ".pdf" => MaterialType.PDF,
            _ => MaterialType.TXT
        };
    }

    /// <summary>
    /// RAG 检索：根据上下文查询相关原作内容
    /// </summary>
    public async Task<List<RAGResult>> RetrieveAsync(string query, NovelProject project, int topK = 5)
    {
        await Task.Delay(300); // 模拟检索延迟

        // 模拟检索结果
        return project.Characters
            .Where(c => query.Contains(c.Name) || c.PersonalityTags.Split(',').Any(t => query.Contains(t.Trim())))
            .Take(topK)
            .Select(c => new RAGResult
            {
                SourceType = "角色设定",
                EntityName = c.Name,
                Content = $"【性格】{c.PersonalityTags}\n【口头禅】{c.Catchphrase}\n【外貌】{c.Appearance}\n【原作引用】{c.SourceQuote}",
                RelevanceScore = 0.95f,
                SourceVolume = c.SourceVolume
            })
            .Concat(project.Locations
                .Where(l => query.Contains(l.Name))
                .Take(topK)
                .Select(l => new RAGResult
                {
                    SourceType = "场景设定",
                    EntityName = l.Name,
                    Content = $"【描述】{l.Description}\n【氛围】{l.Atmosphere}",
                    RelevanceScore = 0.85f,
                    SourceVolume = l.SourceVolume
                }))
            .Take(topK)
            .ToList();
    }

    public List<MaterialDocument> GetDocuments() => _documents;
}

/// <summary>
/// RAG 检索结果
/// </summary>
public class RAGResult
{
    public string SourceType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
    public string SourceVolume { get; set; } = string.Empty;
}
