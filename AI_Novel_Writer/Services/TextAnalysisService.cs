using System.Text.RegularExpressions;
using AINovelWriter.Models;

namespace AINovelWriter.Services;

/// <summary>
/// 智能文本分析服务 —— 通过启发式规则分析用户粘贴的大量文本，
/// 自动提取人物名称、地点、世界观元素，供用户确认后导入项目。
/// 不依赖任何外部 API，纯本地规则引擎。
/// </summary>
public partial class TextAnalysisService
{
    /// <summary>
    /// 解析结果：从文本中提取的各类设定
    /// </summary>
    public class ExtractionResult
    {
        public List<CandidateCharacter> Characters { get; set; } = [];
        public List<CandidateLocation> Locations { get; set; } = [];
        public List<CandidateWorldSetting> WorldSettings { get; set; } = [];
        public string Summary { get; set; } = "";
        /// <summary>原始文本</summary>
        public string SourceText { get; set; } = "";
    }

    public class CandidateCharacter
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public string Tags { get; set; } = "";
        public string Notes { get; set; } = "";
        public int OccurrenceCount { get; set; }
        public string ContextSnippet { get; set; } = "";
        public bool Selected { get; set; } = true;
    }

    public class CandidateLocation
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Selected { get; set; } = true;
    }

    public class CandidateWorldSetting
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Type { get; set; } = "规则";
        public bool Selected { get; set; } = true;
    }

    /// <summary>
    /// 分析大量文本，提取人物/地点/世界观候选
    /// </summary>
    public ExtractionResult Analyze(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ExtractionResult();

        var result = new ExtractionResult
        {
            SourceText = text,
            Summary = GenerateSummary(text)
        };

        // 1. 提取人物候选
        result.Characters = ExtractCharacters(text);

        // 2. 提取地点候选
        result.Locations = ExtractLocations(text);

        // 3. 提取世界观候选（根据特定关键词段落）
        result.WorldSettings = ExtractWorldSettings(text);

        return result;
    }

    /// <summary>
    /// 将提取结果应用到项目模型
    /// </summary>
    public static void ApplyToProject(NovelProject project, ExtractionResult result)
    {
        foreach (var c in result.Characters.Where(c => c.Selected))
        {
            if (!project.Characters.Any(x => x.Name == c.Name))
            {
                project.Characters.Add(new Character
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = c.Name,
                    Role = c.Role,
                    PersonalityTags = c.Tags,
                    Notes = c.Notes
                });
            }
        }

        foreach (var loc in result.Locations.Where(l => l.Selected))
        {
            if (!project.Locations.Any(x => x.Name == loc.Name))
            {
                project.Locations.Add(new Location
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = loc.Name,
                    Category = loc.Category,
                    Description = loc.Description
                });
            }
        }

        foreach (var ws in result.WorldSettings.Where(w => w.Selected))
        {
            if (!project.WorldSettings.Any(x => x.Title == ws.Title))
            {
                project.WorldSettings.Add(new WorldSetting
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Title = ws.Title,
                    Content = ws.Content,
                    Type = ParseWorldType(ws.Type)
                });
            }
        }
    }

    #region 人物提取

    private static List<CandidateCharacter> ExtractCharacters(string text)
    {
        var characters = new List<CandidateCharacter>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // 策略1：识别"姓名：xxx"、"角色：xxx"等显式标记
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 匹配 "姓名：XXX" 或 "名字：XXX" 或 "名称：XXX"
            var nameMatch = NamePattern().Match(trimmed);
            if (nameMatch.Success)
            {
                var name = nameMatch.Groups[1].Value.Trim();
                if (!seen.Contains(name) && name.Length >= 1 && name.Length <= 20)
                {
                    seen.Add(name);
                    var character = new CandidateCharacter
                    {
                        Name = name,
                        Notes = trimmed,
                        ContextSnippet = trimmed.Length > 80 ? trimmed[..80] + "…" : trimmed
                    };

                    // 尝试提取身份
                    var roleMatch = RolePattern().Match(trimmed);
                    if (roleMatch.Success)
                        character.Role = roleMatch.Groups[1].Value.Trim();

                    characters.Add(character);
                }
            }
        }

        // 策略2：使用中文姓名模式匹配（2-3个中文字符，出现在特定语境中）
        var nameCandidates = NameCandidateRegex().Matches(text)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value.Trim())
            .Where(n => n.Length >= 2 && n.Length <= 4 && !seen.Contains(n))
            .Distinct()
            .ToList();

        foreach (var name in nameCandidates)
        {
            if (seen.Contains(name)) continue;
            if (IsCommonWord(name)) continue; // 过滤常见词

            seen.Add(name);
            var context = ExtractContext(text, name, 50);
            characters.Add(new CandidateCharacter
            {
                Name = name,
                Notes = "",
                OccurrenceCount = CountOccurrences(text, name),
                ContextSnippet = context
            });
        }

        return characters;
    }

    #endregion

    #region 地点提取

    private static List<CandidateLocation> ExtractLocations(string text)
    {
        var locations = new List<CandidateLocation>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // "地点：XXX"、"场景：XXX"、"位置：XXX"
            var locMatch = LocationPattern().Match(trimmed);
            if (locMatch.Success)
            {
                var name = locMatch.Groups[1].Value.Trim();
                if (!seen.Contains(name))
                {
                    seen.Add(name);
                    locations.Add(new CandidateLocation
                    {
                        Name = name,
                        Description = trimmed,
                        Category = GuessLocationCategory(name, trimmed)
                    });
                }
            }
        }

        // 策略2：检测"城"、"山"、"河"、"湖"、"岛"、"谷"、"宫"、"殿"、"寺"等地点后缀
        var geoMatches = GeoPattern().Matches(text)
            .Cast<Match>()
            .Select(m => m.Value.Trim())
            .Where(n => n.Length >= 2 && n.Length <= 10 && !seen.Contains(n))
            .Distinct()
            .ToList();

        foreach (var name in geoMatches)
        {
            if (seen.Contains(name)) continue;
            seen.Add(name);
            var context = ExtractContext(text, name, 40);
            locations.Add(new CandidateLocation
            {
                Name = name,
                Description = context,
                Category = GuessLocationCategory(name, context)
            });
        }

        return locations;
    }

    private static string GuessLocationCategory(string name, string context)
    {
        if (name.Contains("城") || name.Contains("都") || name.Contains("市")) return "城市";
        if (name.Contains("山") || name.Contains("峰") || name.Contains("岭")) return "山脉";
        if (name.Contains("河") || name.Contains("江") || name.Contains("海")) return "水域";
        if (name.Contains("宫") || name.Contains("殿") || name.Contains("府")) return "建筑";
        if (name.Contains("国") || name.Contains("洲")) return "地域";
        if (name.Contains("塔") || name.Contains("寺") || name.Contains("庙")) return "地标";
        if (name.Contains("谷") || name.Contains("林") || name.Contains("原")) return "自然地貌";
        return "其他";
    }

    #endregion

    #region 世界观提取

    private static List<CandidateWorldSetting> ExtractWorldSettings(string text)
    {
        var settings = new List<CandidateWorldSetting>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // 按空行分段，分析每个段落
        var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string currentTitle = "";
        string currentContent = "";

        foreach (var line in paragraphs)
        {
            var trimmed = line.Trim();

            // 检测标题行（规则、势力、时间线、特殊设定等关键词开头 + 冒号/分号/空格）
            var wsMatch = WorldSettingHeaderPattern().Match(trimmed);
            if (wsMatch.Success)
            {
                // 保存之前的段落
                if (!string.IsNullOrWhiteSpace(currentTitle) && !string.IsNullOrWhiteSpace(currentContent))
                {
                    AddWorldSetting(settings, seen, currentTitle, currentContent);
                }

                currentTitle = wsMatch.Groups[1].Value.Trim();
                currentContent = wsMatch.Groups[2].Value.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(currentTitle))
            {
                if (!string.IsNullOrWhiteSpace(currentContent))
                    currentContent += "\n" + trimmed;
                else
                    currentContent = trimmed;
            }
        }

        // 最后一段
        if (!string.IsNullOrWhiteSpace(currentTitle) && !string.IsNullOrWhiteSpace(currentContent))
        {
            AddWorldSetting(settings, seen, currentTitle, currentContent);
        }

        // 如果没有匹配到结构化格式，自动从文本中提取含世界观关键词的段落
        if (settings.Count == 0)
        {
            for (int i = 0; i < paragraphs.Length; i++)
            {
                var line = paragraphs[i].Trim();
                if (line.Length < 10) continue;

                // 包含世界观关键词且长度适中的段落
                if (WorldKeywords().IsMatch(line) && line.Length < 200)
                {
                    var title = line.Length > 30 ? line[..30] + "…" : line;
                    if (!seen.Contains(title))
                    {
                        seen.Add(title);
                        settings.Add(new CandidateWorldSetting
                        {
                            Title = title,
                            Content = line,
                            Type = GuessWorldType(line)
                        });
                    }
                }
            }
        }

        return settings;
    }

    private static void AddWorldSetting(List<CandidateWorldSetting> settings, HashSet<string> seen, string title, string content)
    {
        if (!seen.Contains(title))
        {
            seen.Add(title);
            settings.Add(new CandidateWorldSetting
            {
                Title = title,
                Content = content,
                Type = GuessWorldType(title + " " + content)
            });
        }
    }

    private static string GuessWorldType(string text)
    {
        if (text.Contains("规则") || text.Contains("法则") || text.Contains("设定") || text.Contains("系统")) return "规则";
        if (text.Contains("势力") || text.Contains("宗") || text.Contains("门") || text.Contains("教") || text.Contains("族")) return "势力";
        if (text.Contains("时间") || text.Contains("纪元") || text.Contains("年") || text.Contains("时代")) return "时间线";
        if (text.Contains("特殊") || text.Contains("传说") || text.Contains("神话") || text.Contains("秘") || text.Contains("禁")) return "特殊";
        if (text.Contains("地点") || text.Contains("大陆") || text.Contains("域") || text.Contains("界")) return "地点";
        return "规则";
    }

    private static WorldSettingType ParseWorldType(string type)
    {
        return type switch
        {
            "规则" => WorldSettingType.Rule,
            "势力" => WorldSettingType.Faction,
            "地点" => WorldSettingType.Place,
            "时间线" => WorldSettingType.Timeline,
            "特殊" => WorldSettingType.Special,
            _ => WorldSettingType.Rule
        };
    }

    #endregion

    #region 辅助方法

    private static string GenerateSummary(string text)
    {
        // 提取前 200 字作为摘要
        var cleaned = Regex.Replace(text, @"\s+", " ").Trim();
        return cleaned.Length <= 200 ? cleaned : cleaned[..200] + "…";
    }

    private static string ExtractContext(string text, string word, int radius)
    {
        var idx = text.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return "";

        var start = Math.Max(0, idx - radius);
        var end = Math.Min(text.Length, idx + word.Length + radius);
        var ctx = text[start..end].Replace('\n', ' ').Trim();
        if (start > 0) ctx = "…" + ctx;
        if (end < text.Length) ctx += "…";
        return ctx;
    }

    private static int CountOccurrences(string text, string word)
    {
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(word, idx, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            idx += word.Length;
        }
        return count;
    }

    /// <summary>过滤常见中文词语，避免把"因为"、"所以"等误认为姓名</summary>
    private static readonly HashSet<string> CommonWords =
    [
        "因为", "所以", "虽然", "但是", "如果", "可以", "没有", "这个", "那个", "什么", "怎么",
        "他们", "她们", "它们", "我们", "你们", "自己", "时候", "知道", "觉得", "应该",
        "不是", "就是", "只是", "但是", "还是", "或者", "而且", "然后", "之后", "最后",
        "开始", "已经", "一直", "一样", "一起", "一个", "可能", "还有", "之后", "所有",
        "主要", "重要", "基本", "非常", "十分", "比较", "特别", "一定", "看到", "发现",
        "成为", "作为", "而言", "而言", "方面", "对于", "关于", "除了", "根据", "按照",
        "先生", "女士", "小姐"
    ];

    private static bool IsCommonWord(string word) => CommonWords.Contains(word);

    #endregion

    #region 正则

    [GeneratedRegex(@"(?:姓名|名字|名称|角色名|人物|角色[：:]\s*)(\S{1,20})", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"(?:身份|定位|职业|职位|头衔)[：:]\s*(\S{1,20})", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RolePattern();

    [GeneratedRegex(@"(?:地点|场景|位置|所在地)[：:]\s*(\S{1,10})", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex LocationPattern();

    [GeneratedRegex(@"[（(]?(?:称呼|叫|唤作|名为|名叫|称作|人称)(\S{2,4})[）)]", RegexOptions.Compiled)]
    private static partial Regex NameCandidateRegex();

    [GeneratedRegex(@"[一-龥]{2,}(?:城|山|河|湖|海|岛|谷|宫|殿|府|国|洲|塔|寺|庙|林|原|岭|峰|江|港|湾|洞|崖|关|门)", RegexOptions.Compiled)]
    private static partial Regex GeoPattern();

    [GeneratedRegex(@"(?:^|\n)\s*(规则|势力|时间线|特殊设定|世界观|背景设定|法术体系|等级体系|社会结构|地理环境|历史背景|文化传统)[：:、，,\s]+(.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex WorldSettingHeaderPattern();

    [GeneratedRegex(@"(?:世界观|设定|规则|法则|体系|背景|历史|势力|宗门|法术|等级|大陆|世界)", RegexOptions.Compiled)]
    private static partial Regex WorldKeywords();

    #endregion
}
