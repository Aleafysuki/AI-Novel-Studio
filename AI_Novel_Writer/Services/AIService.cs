using AINovelWriter.Models;
using AINovelWriter.Services;

namespace AINovelWriter.Services;

/// <summary>
/// AI 写作引擎服务（原型阶段使用模拟数据）
/// 实际应用中接入 OpenAI/Claude/本地模型 API
/// </summary>
public class AIService
{
    private readonly MaterialService _materialService;
    private readonly string[] _polishTemplates;
    private readonly string[] _generateTemplates;

    public AIService(MaterialService materialService)
    {
        _materialService = materialService;
        _polishTemplates = [
            "她深吸一口气，{0}。空气中弥漫着淡淡的花香，让她的心跳不由自主地加快了。",
            "月光洒在{0}，{1}的身影在夜色中显得格外孤寂。",
            "{0}握紧了拳头，指节因用力而发白。{1}的眼神中闪过一丝决然。",
            "风起了，{0}的衣袂猎猎作响。{1}望向远方，那里有他必须去的地方。",
            "雨滴打在青石板上，发出清脆的声响。{0}站在屋檐下，{1}静静等待。"
        ];
        _generateTemplates = [
            "就在这时，一道身影从暗处走了出来。来人正是{0}，他的目光在{1}身上停留了片刻，缓缓开口道：\"你终于来了。\"",
            "{0}推开沉重的木门，门后是一个他从未见过的世界。{1}的声音从深处传来，带着难以捉摸的情绪。",
            "战斗在一瞬间爆发。{0}的剑快如闪电，但{1}的身体如同鬼魅般闪避。两人之间的对决，已然超越了常人的理解。"
        ];
    }

    /// <summary>
    /// AI 润色选中文本
    /// </summary>
    public async Task<DiffResult> PolishTextAsync(string selectedText, NovelProject project, string currentChapterId)
    {
        await Task.Delay(800);

        var random = new Random();
        var template = _polishTemplates[random.Next(_polishTemplates.Length)];

        // 从上下文中提取可能的人物和地点
        var characters = project.Characters.Where(c =>
            selectedText.Contains(c.Name)).ToList();
        var characterName = characters.Any() ? characters[0].Name : "她";

        var polished = string.Format(template, characterName, characterName);

        var diffService = new DiffService();
        return diffService.ComputeDiff(selectedText, polished, currentChapterId, "polish");
    }

    /// <summary>
    /// AI 生成续写内容
    /// </summary>
    public async Task<string> GenerateContinuationAsync(string context, NovelProject project, string writingGoal)
    {
        await Task.Delay(1500);

        var random = new Random();
        var template = _generateTemplates[random.Next(_generateTemplates.Length)];

        var characters = project.Characters.Take(2).ToList();
        var char1 = characters.Count > 0 ? characters[0].Name : "林逸";
        var char2 = characters.Count > 1 ? characters[1].Name : "苏婉清";

        var generated = string.Format(template, char1, char2);

        // 如果用户提供了写作目标，追加
        if (!string.IsNullOrEmpty(writingGoal))
        {
            generated += $"\n\n（根据您的写作目标「{writingGoal}」，此处将展开相应的情节发展...）";
        }

        return generated;
    }

    /// <summary>
    /// OOC 检查（防人设崩塌）
    /// </summary>
    public async Task<OOCCheckResult> CheckOOCAsync(string content, List<Character> activeCharacters)
    {
        await Task.Delay(500);

        var issues = new List<OOCIssue>();
        var random = new Random();

        foreach (var character in activeCharacters)
        {
            // 模拟检测：检查生成内容中是否出现与角色设定冲突的描述
            var tags = character.PersonalityTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim());

            foreach (var tag in tags)
            {
                // 简单模拟：如果角色标签是"冷酷"但文中出现了"温柔地笑"之类的描述
                if (tag == "冷酷" && content.Contains("温柔"))
                {
                    issues.Add(new OOCIssue
                    {
                        CharacterName = character.Name,
                        SettingTag = tag,
                        ConflictDescription = $"角色设定为「{tag}」，但文中出现「温柔」相关描述，可能偏离人设",
                        Severity = OOCSeverity.Warning
                    });
                }
            }
        }

        return new OOCCheckResult
        {
            HasIssues = issues.Count > 0,
            Issues = issues,
            OverallScore = issues.Count > 0 ? 0.7f : 0.95f,
            Summary = issues.Count > 0
                ? $"检测到 {issues.Count} 处 OOC 风险，建议关注。"
                : "未检测到明显 OOC 问题，角色表现符合设定。"
        };
    }

    /// <summary>
    /// 逻辑一致性检查
    /// </summary>
    public async Task<ConsistencyCheckResult> CheckConsistencyAsync(string content, NovelProject project)
    {
        await Task.Delay(600);

        // 模拟逻辑检查
        var findings = new List<string>();

        // 检查是否有已死亡角色出现
        var deadEvents = project.Events.Where(e => e.Nature == EventNature.Death).ToList();
        foreach (var deadEvent in deadEvents)
        {
            var deadChars = project.Characters.Where(c => deadEvent.InvolvedCharacterIds.Contains(c.Id));
            foreach (var char_ in deadChars)
            {
                if (content.Contains(char_.Name, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add($"⚠️ 角色「{char_.Name}」在第{deadEvent.ChapterId}章已死亡，但当前内容中出现该角色。");
                }
            }
        }

        return new ConsistencyCheckResult
        {
            IsConsistent = findings.Count == 0,
            Findings = findings,
            Suggestion = findings.Count > 0
                ? "检测到逻辑矛盾，建议修改相关段落或检查事件状态。"
                : "内容逻辑一致，未发现矛盾。"
        };
    }

    /// <summary>
    /// 生成章节摘要
    /// </summary>
    public async Task<string> SummarizeChapterAsync(string content, List<Character> characters)
    {
        await Task.Delay(400);

        var characterNames = characters.Take(3).Select(c => c.Name);
        var words = content.Length;
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

        return $"本章共 {words} 字，{lines} 段。主要出场人物：{string.Join("、", characterNames)}。"
               + "情节概要：主角在关键时刻做出了重要抉择，推动了故事的转折。";
    }

    // ==================== V2：AI 生成块 ====================

    /// <summary>
    /// 根据快捷命令生成一个 AI 生成块（续写/扩写/改对白等 20+ 命令）
    /// </summary>
    /// <param name="instruction">用户可选的修改意见（来自编辑器输入）</param>
    public async Task<AIBlock> GenerateBlockAsync(
        AIBlockKind kind, string sourceText, NovelProject project, double participation,
        string? instruction = null)
    {
        // 参与度越高，生成越"大胆"、越长；这里用延迟模拟不同强度
        await Task.Delay(600 + (int)(participation * 8));

        var chars = project.Characters.Take(2).ToList();
        var c1 = chars.Count > 0 ? chars[0].Name : "林逸";
        var c2 = chars.Count > 1 ? chars[1].Name : "苏婉清";

        string text = kind switch
        {
            AIBlockKind.Continue =>
                $"话音落下，{c1}缓缓拔出背后的长剑。剑身出鞘的清鸣声在山谷间回荡，惊起一片飞鸟。他的目光越过重重迷雾，落在远方那座若隐若现的城池上——临渊城，他此行的目的地。\n\n\u201c无论前方有什么在等着我，\u201d他低声自语，\u201c剑道之路，唯有向前。\u201d",
            AIBlockKind.Expand =>
                $"{sourceText}\n\n更进一步说，此刻的空气仿佛凝固了。远处的松涛声、近旁的溪流声，甚至是自己的心跳声，都被无限放大。{c1}能清晰地感觉到掌心沁出的细汗，以及那股顺着脊背缓缓爬升的寒意。这种感觉，他并不陌生——那是危险临近的征兆。",
            AIBlockKind.Abbreviate =>
                $"{c1}拔剑，望向临渊城，低声道：\u201c剑道之路，唯有向前。\u201d",
            AIBlockKind.Rewrite =>
                $"长剑出鞘，寒光乍现。{c1}的身形在暮色中拉出一道长长的剪影，宛如一柄即将出鞘的名剑，蓄势待发。他没有回头，只是一步步朝着临渊城的方向走去。",
            AIBlockKind.ChangeDialogue =>
                $"\u201c你以为，凭你也能拦得住我？\u201d{c1}嘴角扬起一抹冷冽的弧度，\u201c让开，我没工夫陪你耗。\u201d\n\u201c好大的口气。\u201d对面之人冷笑，\u201c那就让我看看，你有没有这个本事。\u201d",
            AIBlockKind.ChangeDescription =>
                $"夕阳的余晖洒在斑驳的城墙上，为这座古老的城池镀上了一层暗金色的光晕。城门口人来人往，商贩的吆喝声、马车的辘辘声交织成一片喧嚣，与城外万妖山脉的死寂形成了鲜明的对比。",
            AIBlockKind.AddDetail =>
                $"{sourceText}\n\n他的指尖轻轻拂过剑柄上那道细微的裂痕——那是三年前那场生死之战留下的印记。剑穗早已褪色，边缘磨得起了毛边，却被他用一根崭新的红绳仔细系好。",
            AIBlockKind.AddConflict =>
                $"{sourceText}\n\n就在这时，一柄泛着幽蓝寒芒的飞刀毫无征兆地破空而至，直取{c1}咽喉！他侧身闪避，飞刀擦着脖颈钉入身后的树干，刀柄兀自嗡嗡作响。\u201c谁？！\u201d{c1}厉喝一声，剑已出鞘。",
            AIBlockKind.AddPsychology =>
                $"{sourceText}\n\n{c1}的内心却并不如表面这般平静。被逐出师门的屈辱、对未来的迷茫、还有那一丝连他自己都不愿承认的恐惧，此刻正在胸腔里翻涌。可他不能退。退一步，便是万丈深渊。",
            AIBlockKind.AddForeshadow =>
                $"{sourceText}\n\n临行前，{c2}将一枚温润的玉佩塞进他手心，欲言又止。\u201c若有一天你到了帝都……\u201d她话到嘴边，却终究只是摇了摇头，\u201c算了，你会明白的。\u201d那枚玉佩上，隐约刻着一个他看不懂的古老纹章。",
            AIBlockKind.AdjustPace =>
                $"剑光。血花。惨叫。\n一切发生得太快。\n{c1}收剑而立，衣袂无风自动。地上，三具尸体尚有余温。\n他没有多看一眼，转身，融入夜色。",
            AIBlockKind.ToFirstPerson =>
                $"我缓缓拔出背后的长剑，剑身出鞘的清鸣在山谷间回荡。我望向远方那座若隐若现的城池——临渊城，我此行的目的地。无论前方有什么在等着我，剑道之路，我唯有向前。",
            AIBlockKind.ToThirdPerson =>
                $"{c1}缓缓拔出背后的长剑，剑身出鞘的清鸣在山谷间回荡。他望向远方那座若隐若现的城池——临渊城，他此行的目的地。无论前方有什么在等着他，剑道之路，他唯有向前。",
            _ => "（AI 生成的内容）"
        };

        // 将用户的修改意见带入生成结果，便于在原型中直观看到"意见被采纳"
        if (!string.IsNullOrWhiteSpace(instruction))
            text += $"\n\n（已参考您的修改意见：「{instruction}」）";

        // 套用写作风格的用户预设内容（在设置中填写）
        var presetContent = AppState.Settings.GetPresetContent(project.Prompt.Style);
        if (!string.IsNullOrWhiteSpace(presetContent))
            text += $"\n\n（已套用风格预设「{project.Prompt.Style}」：{presetContent}）";

        return new AIBlock
        {
            Kind = kind,
            SourceText = sourceText,
            GeneratedText = text,
            ParticipationSnapshot = participation,
            Explanation = BuildExplanation(kind, project, instruction),
            Status = AIBlockStatus.Pending
        };
    }

    private static string BuildExplanation(AIBlockKind kind, NovelProject project, string? instruction = null)
    {
        var world = project.WorldSettings.FirstOrDefault(w => w.IsLocked);
        var worldNote = world != null ? $"，并遵守世界观设定「{world.Title}」" : "";
        var instrNote = !string.IsNullOrWhiteSpace(instruction) ? $"（已采纳修改意见：「{instruction}」）" : "";
        return kind switch
        {
            AIBlockKind.Continue => $"依据前文语境与主角「{(project.Characters.FirstOrDefault()?.Name ?? "主角")}」的性格续写{worldNote}。{instrNote}",
            AIBlockKind.AddConflict => "在平缓段落后加入突发冲突，提升章节张力与读者留存。",
            AIBlockKind.AddForeshadow => "埋设与后续帝都剧情相关的伏笔，为长线叙事做铺垫。",
            AIBlockKind.ToFirstPerson => "已将叙事人称由第三人称转为第一人称，保持时态与语气一致。",
            _ => $"根据「{kind}」指令改写选中文本，保持人物一致性{worldNote}。{instrNote}"
        };
    }

    // ==================== V2：大纲规划 ====================

    /// <summary>
    /// 由一句话创意生成小说大纲（卷 → 章）
    /// </summary>
    public async Task<List<OutlineNode>> GenerateOutlineAsync(string idea, NovelProject project)
    {
        await Task.Delay(1500);

        var volume1 = new OutlineNode
        {
            Level = OutlineLevel.Volume,
            Title = "第一卷 · 剑出青云",
            Summary = $"围绕创意「{idea}」展开：主角遭遇变故，踏上旅程，在磨难中获得机缘并结识关键伙伴，初步建立目标。",
            KeyCharacters = "主角、女主、竞争对手"
        };
        volume1.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第一章 · 变故骤起", Summary = "主角遭遇人生重大变故，被迫离开原有环境，埋下复仇/证明自我的种子。", KeyCharacters = "主角" });
        volume1.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第二章 · 绝境奇遇", Summary = "主角在险境中获得改变命运的机缘（传承/宝物/能力）。", KeyCharacters = "主角" });
        volume1.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第三章 · 初入江湖", Summary = "主角进入更大的世界，结识女主，卷入势力纷争。", KeyCharacters = "主角、女主" });
        volume1.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第四章 · 一战成名", Summary = "主角在公开场合展露锋芒，一战成名，同时招致强敌注意。", KeyCharacters = "主角、竞争对手" });

        var volume2 = new OutlineNode
        {
            Level = OutlineLevel.Volume,
            Title = "第二卷 · 风云际会",
            Summary = "主角实力提升，卷入更高层级的势力博弈，逐步揭开身世/变故背后的真相。",
            KeyCharacters = "主角、女主、幕后势力"
        };
        volume2.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第五章 · 暗流涌动", Summary = "隐藏势力开始布局，主角成为棋局中的关键一子。", KeyCharacters = "主角、幕后势力" });
        volume2.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第六章 · 真相初现", Summary = "主角逐步逼近当年变故的真相，情感线与主线交织推进。", KeyCharacters = "主角、女主" });

        return [volume1, volume2];
    }

    /// <summary>
    /// 由章节大纲生成正文
    /// </summary>
    public async Task<string> GenerateBodyFromOutlineAsync(OutlineNode chapterNode, NovelProject project)
    {
        await Task.Delay(1800);
        var c1 = project.Characters.FirstOrDefault()?.Name ?? "林逸";
        return $"""
                {chapterNode.Title.Replace("·", "").Trim()}

                {chapterNode.Summary}

                夜色如墨，{c1}独自立于山巅。冷风卷起他的衣袍，猎猎作响。他回想起这一路走来的种种，心中五味杂陈。

                \u201c既然选择了这条路，就没有回头的道理。\u201d他握紧手中长剑，眼神愈发坚定。

                远方，临渊城的灯火在夜幕中明明灭灭，宛如一双注视着他的眼睛。属于他的传奇，即将在那里拉开序幕。

                （——本段由 AI 依据章节大纲自动生成，参与度 {project.Participation.Level:0}%，可继续修改或续写——）
                """;
    }
}

/// <summary>
/// OOC 检查结果
/// </summary>
public class OOCCheckResult
{
    public bool HasIssues { get; set; }
    public List<OOCIssue> Issues { get; set; } = [];
    public float OverallScore { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class OOCIssue
{
    public string CharacterName { get; set; } = string.Empty;
    public string SettingTag { get; set; } = string.Empty;
    public string ConflictDescription { get; set; } = string.Empty;
    public OOCSeverity Severity { get; set; }
}

public enum OOCSeverity
{
    Info,
    Warning,
    Critical
}

public class ConsistencyCheckResult
{
    public bool IsConsistent { get; set; }
    public List<string> Findings { get; set; } = [];
    public string Suggestion { get; set; } = string.Empty;
}
