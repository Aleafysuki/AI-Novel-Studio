using AINovelWriter.Models;
using System.Text.Json;
using Windows.Storage;

namespace AINovelWriter.Services;

/// <summary>
/// 项目管理服务：创建示例项目、保存/加载
/// </summary>
public class ProjectService
{
    /// <summary>
    /// 创建一个完整的示例项目
    /// </summary>
    public NovelProject CreateSampleProject()
    {
        var project = new NovelProject
        {
            Title = "剑道独尊 · 外传",
            OriginalWork = "剑道独尊",
            Synopsis = "叶尘离开天武大陆百年后，新一代剑客崛起。林逸，一个被逐出师门的少年剑客，在命运的洪流中踏上追寻剑道极致的旅程。本作延续剑道独尊世界观，讲述全新的剑道传奇。",
            Outline = @"第一卷：剑出青云
第一章：逐出师门 - 林逸因触犯门规被逐出青云宗，在万妖山脉边缘遇险。
第二章：奇遇 - 意外发现上古剑修洞府，获得剑道传承。
第三章：初入江湖 - 进入临渊城，结识神秘少女苏婉清。
第四章：剑会风云 - 参加临渊城剑会，一战成名。
第五章：暗流涌动 - 天剑门暗中布局，林逸成为棋子。

第二卷：帝都风云
第六章：帝都之路 - 前往帝都，途中遭遇天剑门截杀。
第七章：朝堂之上 - 揭示帝都各方势力的暗中角逐。
第八章：真相大白 - 发现当年被逐出师门的真相。",
            Tags = ["武侠", "玄幻", "剑道", "同人"],
            WritingGoal = "创作林逸在临渊城的首次冒险",
            Genre = "玄幻武侠",
            Mode = CreationMode.FanFiction,
            TargetPlatform = "起点中文网",
            AiStyle = "武侠"
        };

        // AI 参与度默认设为"协同创作"
        project.Participation.Level = 55;
        project.Participation.NotifyDerived();

        // Prompt 可视化配置
        project.Prompt.Style = "武侠";
        project.Prompt.CharacterConsistency = "高";
        project.Prompt.CreativeFreedom = "适当发挥";
        project.Prompt.TargetPlatform = "起点中文网";

        // 世界观设定（独立管理）
        project.WorldSettings.Add(new WorldSetting
        {
            Type = WorldSettingType.Rule, Title = "剑道修行体系",
            Content = "修行分为炼体、剑徒、剑士、剑师、剑宗、剑尊、剑圣七境。每一境界又分初、中、后三期。突破需感悟对应剑意。",
            IsLocked = true
        });
        project.WorldSettings.Add(new WorldSetting
        {
            Type = WorldSettingType.Faction, Title = "青云宗",
            Content = "天下剑修圣地，正道领袖，严禁弟子修习魔道功法。掌门为剑圣境强者。",
            IsLocked = true
        });
        project.WorldSettings.Add(new WorldSetting
        {
            Type = WorldSettingType.Faction, Title = "天剑门",
            Content = "新兴剑道势力，野心勃勃，暗中觊觎青云宗地位。门中天才辈出，陈枫为其代表。",
            IsLocked = true
        });
        project.WorldSettings.Add(new WorldSetting
        {
            Type = WorldSettingType.Special, Title = "九幽剑诀",
            Content = "魔道剑尊所创的禁忌剑法，威力极强但易受心魔侵蚀。青云宗立宗千年严禁修习。主角意外获得残篇。",
            IsLocked = true
        });
        project.WorldSettings.Add(new WorldSetting
        {
            Type = WorldSettingType.Timeline, Title = "时代背景",
            Content = "叶尘飞升百年后的天武大陆，正魔两道暂时休战，但暗流涌动，新一代剑客正在崛起。",
            IsLocked = false
        });

        // 创建角色
        var linYi = new Character
        {
            Name = "林逸", Alias = "剑痴",
            Role = "主角", IsOriginalCharacter = true,
            PersonalityTags = "坚韧,内敛,执着,重情义",
            Catchphrase = "剑道之路，唯有向前。",
            Appearance = "十七八岁少年，剑眉星目，一袭青衫，背负古朴长剑。",
            Background = "青云宗外门弟子，因私自修炼禁忌剑法被逐出师门。出身平凡，但天赋异禀。",
            Abilities = "禁忌剑法·残篇、青云剑法（精通）、身法灵动",
            Motivation = "证明自己的剑道，找到当年陷害自己的真凶。",
            SourceQuote = "「叶尘前辈留下的剑道，不会被埋没。」"
        };

        var suWanQing = new Character
        {
            Name = "苏婉清", Alias = "月下剑仙",
            Role = "女主角", IsOriginalCharacter = true,
            PersonalityTags = "聪慧,冷静,神秘,外表温柔内心刚强",
            Catchphrase = "世间之事，不问对错，只问本心。",
            Appearance = "白衣胜雪，青丝如瀑，眼眸如秋水。",
            Background = "来历不明的神秘女子，似乎与帝都有千丝万缕的联系。",
            Abilities = "月华剑诀、琴心剑法",
            Motivation = "调查家族灭门真相。",
            Relationships =
            [
                new CharacterRelationship
                {
                    TargetCharacterId = "",
                    TargetName = "林逸",
                    Relationship = "同伴",
                    Description = "在临渊城相遇后结伴同行，彼此信任。"
                }
            ]
        };

        var chenFeng = new Character
        {
            Name = "陈枫",
            Role = "配角/竞争对手",
            IsOriginalCharacter = true,
            PersonalityTags = "傲慢,好胜,外冷内热",
            Catchphrase = "剑下见真章。",
            Appearance = "红发张扬，目光如炬，身着火红战袍。",
            Background = "天剑门内门弟子，被誉为百年难遇的剑道天才。",
            Abilities = "天剑诀、焚天剑法",
            Motivation = "击败林逸，证明天剑门的剑道才是正统。"
        };

        project.Characters.Add(linYi);
        project.Characters.Add(suWanQing);
        project.Characters.Add(chenFeng);

        // 创建场景
        project.Locations =
        [
            new Location
            {
                Name = "万妖山脉", Category = "自然景观",
                Description = "横亘大陆的巨型山脉，妖兽横行，也是上古遗迹遍布之地。",
                Atmosphere = "阴森诡谲，灵气紊乱",
                IsOriginalLocation = true, SourceVolume = "剑道独尊 第一卷"
            },
            new Location
            {
                Name = "临渊城", Category = "城市",
                Description = "帝国南部重镇，城墙高达百丈，号称\"不落之城\"。城中商贾云集，各方势力盘踞。",
                Atmosphere = "繁华与危险并存",
                IsOriginalLocation = false
            },
            new Location
            {
                Name = "青云宗", Category = "建筑",
                Description = "天下剑修圣地，建于青云峰之巅，云雾缭绕。",
                Atmosphere = "庄严肃穆，剑气冲霄",
                IsOriginalLocation = true, SourceVolume = "剑道独尊 第一卷"
            }
        ];

        // 创建章节（草稿）
        project.Chapters =
        [
            new Chapter { Title = "逐出师门", Order = 1, Status = ChapterStatus.Completed, WordCount = 3500,
                AiGeneratedWords = 2100, AiSummary = "林逸因修炼禁忌《九幽剑诀》被青云宗逐出师门，独自踏上未知旅程。",
                Content = GetChapter1Content() },
            new Chapter { Title = "奇遇", Order = 2, Status = ChapterStatus.Writing, WordCount = 1200,
                AiGeneratedWords = 900, AiSummary = "林逸在万妖山脉发现上古剑修洞府，接近改变命运的机缘。",
                Content = GetChapter2Content() },
            new Chapter { Title = "初入江湖", Order = 3, Status = ChapterStatus.Draft,
                AiSummary = "林逸抵达临渊城，结识神秘少女苏婉清。" },
            new Chapter { Title = "剑会风云", Order = 4, Status = ChapterStatus.Draft,
                AiSummary = "临渊城剑会，林逸一战成名，与陈枫结下梁子。" }
        ];

        // 大纲树（卷 → 章），部分已确认/已生成
        var vol1 = new OutlineNode
        {
            Level = OutlineLevel.Volume, Title = "第一卷 · 剑出青云", IsConfirmed = true,
            Summary = "林逸被逐出师门，于万妖山脉获得剑道传承，初入临渊城崭露头角。",
            KeyCharacters = "林逸、苏婉清、陈枫"
        };
        vol1.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第一章 · 逐出师门", Summary = "林逸因修习禁术被逐出青云宗。", KeyCharacters = "林逸", IsConfirmed = true, HasContent = true, LinkedChapterId = project.Chapters[0].Id });
        vol1.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第二章 · 奇遇", Summary = "发现上古剑修洞府，获得传承。", KeyCharacters = "林逸", IsConfirmed = true, HasContent = true, LinkedChapterId = project.Chapters[1].Id });
        vol1.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第三章 · 初入江湖", Summary = "进入临渊城，结识苏婉清。", KeyCharacters = "林逸、苏婉清", IsConfirmed = true });
        vol1.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第四章 · 剑会风云", Summary = "临渊城剑会一战成名。", KeyCharacters = "林逸、陈枫" });
        project.Outlines.Add(vol1);

        var vol2 = new OutlineNode
        {
            Level = OutlineLevel.Volume, Title = "第二卷 · 帝都风云",
            Summary = "林逸前往帝都，卷入各方势力博弈，揭开被逐出师门的真相。",
            KeyCharacters = "林逸、苏婉清、天剑门"
        };
        vol2.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第五章 · 暗流涌动", Summary = "天剑门暗中布局。", KeyCharacters = "林逸" });
        vol2.Children.Add(new OutlineNode { Level = OutlineLevel.Chapter, Title = "第六章 · 帝都之路", Summary = "赴帝都途中遭截杀。", KeyCharacters = "林逸、苏婉清" });
        project.Outlines.Add(vol2);

        // AI 操作历史（示例）
        project.History.Add(new AIHistoryRecord
        {
            ChapterId = project.Chapters[1].Id, Title = "续写 · 洞府探秘",
            ActionLabel = "续写", Command = "AI 续写", AffectedWords = 320, IsCurrent = true,
            CreatedAt = DateTime.Now.AddMinutes(-8)
        });
        project.History.Add(new AIHistoryRecord
        {
            ChapterId = project.Chapters[0].Id, Title = "改描写 · 大殿场景",
            ActionLabel = "改描写", Command = "改描写", AffectedWords = 156,
            CreatedAt = DateTime.Now.AddMinutes(-25)
        });

        // 创建关键事件
        project.Events =
        [
            new NovelEvent
            {
                Title = "林逸被逐出师门", Description = "因修炼禁忌剑法被青云宗长老发现，遭逐出师门。",
                ChapterId = project.Chapters[0].Id, Nature = EventNature.Separation,
                InvolvedCharacterIds = [linYi.Id],
                DependentEventIds = []
            },
            new NovelEvent
            {
                Title = "上古剑修洞府", Description = "在万妖山脉发现上古剑修遗留的洞府，获得剑道传承。",
                ChapterId = project.Chapters[1].Id, Nature = EventNature.Discovery,
                InvolvedCharacterIds = [linYi.Id]
            },
            new NovelEvent
            {
                Title = "林逸与苏婉清相遇", Description = "在临渊城首次相遇，苏婉清出手相助。",
                ChapterId = project.Chapters[2].Id, Nature = EventNature.Meeting,
                InvolvedCharacterIds = [linYi.Id, suWanQing.Id]
            }
        ];

        // 建立事件依赖关系
        project.Events[1].PrerequisiteEventIds = [project.Events[0].Id];
        project.Events[2].PrerequisiteEventIds = [project.Events[1].Id];
        project.Events[0].DependentEventIds = [project.Events[1].Id, project.Events[2].Id];

        // 更新角色关系中的 ID
        suWanQing.Relationships[0].TargetCharacterId = linYi.Id;
        linYi.Relationships =
        [
            new CharacterRelationship
            {
                TargetCharacterId = suWanQing.Id, TargetName = "苏婉清",
                Relationship = "同伴", Description = "在临渊城相遇后结伴同行。"
            },
            new CharacterRelationship
            {
                TargetCharacterId = chenFeng.Id, TargetName = "陈枫",
                Relationship = "对手", Description = "剑道上的竞争对手。"
            }
        ];

        project.CurrentChapterId = project.Chapters[1].Id;
        project.CurrentCharacterId = linYi.Id;

        return project;
    }

    // ==================== 项目持久化（JSON） ====================

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>将项目序列化并写入指定文件</summary>
    public void SaveToFile(NovelProject project, StorageFile file)
    {
        project.LastModifiedAt = DateTime.Now;
        project.TotalWords = project.Chapters.Sum(c => c.WordCount);
        var json = JsonSerializer.Serialize(project, JsonOptions);
        FileIO.WriteTextAsync(file, json).AsTask().Wait();
    }

    /// <summary>从文件反序列化项目</summary>
    public NovelProject? LoadFromFile(StorageFile file)
    {
        var json = FileIO.ReadTextAsync(file).AsTask().Result;
        var project = JsonSerializer.Deserialize<NovelProject>(json);
        if (project == null) return null;

        // 反序列化后重建轻量派生关系
        if (string.IsNullOrEmpty(project.CurrentChapterId) && project.Chapters.Count > 0)
            project.CurrentChapterId = project.Chapters[0].Id;
        project.Participation.NotifyDerived();
        return project;
    }

    /// <summary>
    /// 由创建向导参数创建一个全新的空项目
    /// </summary>
    public NovelProject CreateFromWizard(
        string title, string genre, CreationMode mode, string originalWork,
        string worldview, string targetPlatform, string aiStyle, double participation)
    {
        var project = new NovelProject
        {
            Title = string.IsNullOrWhiteSpace(title) ? "未命名作品" : title,
            Genre = genre,
            Mode = mode,
            OriginalWork = mode == CreationMode.FanFiction ? originalWork : string.Empty,
            TargetPlatform = targetPlatform,
            AiStyle = aiStyle,
            Synopsis = string.IsNullOrWhiteSpace(worldview) ? "（尚未填写简介）" : worldview,
            Tags = [genre, mode == CreationMode.FanFiction ? "同人" : "原创"]
        };

        project.Participation.Level = participation;
        project.Participation.NotifyDerived();
        project.Prompt.Style = aiStyle;
        project.Prompt.TargetPlatform = targetPlatform;

        if (!string.IsNullOrWhiteSpace(worldview))
        {
            project.WorldSettings.Add(new WorldSetting
            {
                Type = WorldSettingType.Special, Title = "核心世界观",
                Content = worldview, IsLocked = true
            });
        }

        // 创建一个空的起始章节
        var first = new Chapter { Title = "第一章", Order = 1, Status = ChapterStatus.Draft };
        project.Chapters.Add(first);
        project.CurrentChapterId = first.Id;

        return project;
    }

    private static string GetChapter1Content()
    {
        return """
                青云峰顶，剑气纵横。
                
                林逸站在大殿中央，承受着来自四面八方的目光。有惋惜，有嘲讽，更多的是冷漠。
                
                "林逸，你可知罪？"大长老的声音如同洪钟，在大殿中回荡。
                
                林逸抬起头，目光平静地扫过在场的每一个人。他知道，这一天迟早会来。从他偷偷进入藏经阁顶层，翻阅那本禁忌剑谱的那一刻起，他就已经做好了准备。
                
                "弟子不知。"他的声音不大，却清晰地传入每个人耳中。
                
                大长老的眉头皱得更深了。他抚摸着手中的玉简——那是林逸修炼禁忌剑法的证据。"你私下修习《九幽剑诀》，此乃本门禁术。按门规，当废去修为，逐出师门。"
                
                人群中响起一阵窃窃私语。《九幽剑诀》——传说中由魔道剑尊所创的剑法，曾在大陆上掀起腥风血雨。青云宗立宗千年，严禁弟子修习。
                
                林逸握紧了拳头。那是一年前的事了。他在万妖山脉历练时，无意中从一具骸骨旁发现了这本剑谱。剑谱上的字迹仿佛有魔力，让他无法移开目光。
                
                他练了，而且进步神速。
                
                "大长老，"林逸开口了，声音中带着一丝倔强，"剑法本身并无正邪之分。九幽剑诀虽为魔道所创，但其中蕴含的剑理，难道不值得我们思考吗？"
                
                "放肆！"另一位长老拍案而起，"魔道剑法你也敢妄加评论？青云宗数百年清誉，岂容你玷污！"
                
                林逸沉默了。他知道，说什么都没用。规矩就是规矩。
                
                大长老叹了口气："念你曾是外门弟子中的佼佼者，免去废功之刑。但青云宗，容不下你了。"
                
                林逸解下腰间的身份玉牌，轻轻放在地上。
                
                转身走出大殿时，他听见身后传来一声轻语：
                
                "林逸，剑道之路很长，好自为之。"
                
                他没有回头。山风呼啸，吹起他的青衫衣角。远处，万妖山脉的轮廓在夕阳下显得格外狰狞。
                
                剑道之路，唯有向前。
                """;
    }

    private static string GetChapter2Content()
    {
        return """
                万妖山脉的夜晚，比林逸想象中更加危险。

                离开青云宗已经三天了。他沿着山脉边缘前行，试图找到通往临渊城的路。但妖兽的嚎叫从未停止，空气中弥漫着腐叶和血腥的气息。

                天色渐暗时，林逸找到一处隐蔽的山洞。洞内漆黑一片，但他能感觉到，这里曾经有人来过——地面上散落着几块碎裂的玉简。

                他点亮火折子，小心翼翼地往深处走去。洞壁上刻满了古老的符文，散发着微弱的荧光。

                "这是...上古剑修的洞府？"

                林逸的心跳加快了。在青云宗的藏经阁中，他曾读到过关于上古剑修的记载——那是一群追求剑道极致的疯子，为了一招剑式可以闭关百年。

                洞府深处，一具骸骨盘膝而坐。骸骨身前放着一枚玉简和一柄断剑。

                林逸屏住呼吸，缓缓走近。
                """;
    }
}
