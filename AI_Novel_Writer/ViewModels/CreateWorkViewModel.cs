using CommunityToolkit.Mvvm.ComponentModel;
using AINovelWriter.Models;

namespace AINovelWriter.ViewModels;

/// <summary>
/// 作品创建向导 ViewModel
/// </summary>
public partial class CreateWorkViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = "";

    [ObservableProperty]
    public partial string Genre { get; set; } = "玄幻";

    public List<string> GenreOptions { get; } =
        ["玄幻", "仙侠", "武侠", "都市", "科幻", "历史", "悬疑", "游戏", "言情", "奇幻", "轻小说"];

    /// <summary>
    /// 是否同人（否则为原创）
    /// </summary>
    [ObservableProperty]
    public partial bool IsFanFiction { get; set; }

    [ObservableProperty]
    public partial string OriginalWork { get; set; } = "";

    [ObservableProperty]
    public partial string Worldview { get; set; } = "";

    [ObservableProperty]
    public partial string TargetPlatform { get; set; } = "起点中文网";

    public List<string> PlatformOptions { get; } =
        ["起点中文网", "番茄小说", "晋江文学城", "纵横中文网", "QQ阅读", "七猫小说", "其他/暂不确定"];

    [ObservableProperty]
    public partial string AiStyle { get; set; } = "轻小说";

    public List<string> StyleOptions => AINovelWriter.Services.AppState.Settings.AllStyles();

    /// <summary>
    /// 初始 AI 参与度
    /// </summary>
    [ObservableProperty]
    public partial double Participation { get; set; } = 55;

    public string ParticipationMode => Participation switch
    {
        <= 5 => "纯人工模式",
        <= 35 => "轻度辅助",
        <= 65 => "协同创作",
        <= 90 => "AI 主写",
        _ => "全自动创作"
    };

    public string ParticipationDesc => Participation switch
    {
        <= 5 => "AI 仅提供润色与设定冲突提醒，不主动生成正文。",
        <= 35 => "AI 可续写短句、提供剧情方向建议，正文以你为主。",
        <= 65 => "你与 AI 一人一句共同创作，AI 主动补充描写与对白。",
        <= 90 => "你给出目标与概要，AI 自动写完整正文，你负责修改把控。",
        _ => "你只需提供世界观与大纲，AI 自动完成分章与正文。"
    };

    public bool CanCreate => !string.IsNullOrWhiteSpace(Title);

    partial void OnParticipationChanged(double value)
    {
        OnPropertyChanged(nameof(ParticipationMode));
        OnPropertyChanged(nameof(ParticipationDesc));
    }

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(CanCreate));

    public NovelProject Build()
    {
        return AINovelWriter.Services.AppState.ProjectService.CreateFromWizard(
            Title, Genre,
            IsFanFiction ? CreationMode.FanFiction : CreationMode.Original,
            OriginalWork, Worldview, TargetPlatform, AiStyle, Participation);
    }
}
