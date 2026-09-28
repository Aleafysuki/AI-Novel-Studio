using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using AINovelWriter.Models;
using AINovelWriter.Services;

namespace AINovelWriter.ViewModels;

/// <summary>
/// Cursor 式编辑器 ViewModel —— AI 生成 / 人类掌控 的核心
/// </summary>
public partial class EditorViewModel : ObservableObject
{
    private readonly AIService _ai = AppState.AIService;
    private readonly DiffService _diff = AppState.DiffService;

    public EditorViewModel()
    {
        Project = AppState.CurrentProject ?? AppState.ProjectService.CreateSampleProject();
        AppState.CurrentProject = Project;

        GlobalParticipation = Project.Participation.Level;

        var current = Project.Chapters.FirstOrDefault(c => c.Id == Project.CurrentChapterId)
                      ?? Project.Chapters.FirstOrDefault();
        if (current != null) SelectChapter(current);

        AppState.HasUnsavedChanges = false;
        AppState.AutoSaveService.Start(Project);

        Log("项目已加载：" + Project.Title);
    }

    // ================= 项目 / 章节 =================
    public NovelProject Project { get; }
    public ObservableCollection<Chapter> Chapters => Project.Chapters;
    public ObservableCollection<WorldSetting> WorldSettings => Project.WorldSettings;
    public ObservableCollection<AIHistoryRecord> History => Project.History;
    public ObservableCollection<Character> Characters => Project.Characters;

    /// <summary>写作风格下拉：默认风格 + 用户在设置中自定义的风格</summary>
    public List<string> StyleOptions => AppState.Settings.AllStyles();

    [ObservableProperty]
    public partial Chapter? CurrentChapter { get; set; }

    [ObservableProperty]
    public partial string EditorContent { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedText { get; set; } = "";

    public int SelectionStart { get; set; }
    public int SelectionLength { get; set; }
    public int CursorPosition { get; set; }

    public bool HasSelection => !string.IsNullOrEmpty(SelectedText);

    // ================= AI 参与度 =================
    [ObservableProperty]
    public partial double GlobalParticipation { get; set; } = 30;

    [ObservableProperty]
    public partial bool UseChapterOverride { get; set; }

    [ObservableProperty]
    public partial double ChapterParticipation { get; set; } = 50;

    public double EffectiveParticipation => UseChapterOverride ? ChapterParticipation : GlobalParticipation;

    public string ParticipationMode => ModeOf(EffectiveParticipation);
    public string ParticipationDesc => DescOf(EffectiveParticipation);
    public double EffectiveForColor => EffectiveParticipation;

    // 能力矩阵（跟随有效参与度）
    public bool CanAutoContinue => EffectiveParticipation > 20;
    public bool CanAutoExpand => EffectiveParticipation > 15;
    public bool CanAutoBuildSetting => EffectiveParticipation > 45;
    public bool CanAdvancePlot => EffectiveParticipation > 45;
    public bool CanCompleteChapter => EffectiveParticipation > 70;

    // ================= AI 生成块（右栏） =================
    public ObservableCollection<AIBlock> PendingBlocks { get; } = [];

    // ================= Diff 状态 =================
    [ObservableProperty]
    public partial bool IsDiffMode { get; set; }

    public ObservableCollection<DiffLineVm> DiffLines { get; } = [];

    [ObservableProperty]
    public partial string DiffTitle { get; set; } = "";

    [ObservableProperty]
    public partial string DiffExplanation { get; set; } = "";

    private string _diffProposed = "";
    private AIBlockKind _diffKind;

    // ================= UI 状态 =================
    [ObservableProperty]
    public partial bool IsAiBusy { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "就绪";

    [ObservableProperty]
    public partial string LogText { get; set; } = "";

    [ObservableProperty]
    public partial int RightTabIndex { get; set; }

    // ================= 联网搜索 =================
    [ObservableProperty]
    public partial string SearchQuery { get; set; } = "";

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial string SearchStatus { get; set; } = "";

    public ObservableCollection<WebSearchResult> SearchResults { get; } = [];

    [ObservableProperty]
    public partial WebPageContent? SelectedPageContent { get; set; }

    private readonly WebSearchService _webSearch = AppState.WebSearchService;

    // 快捷命令清单（选中文本后可用）
    public List<QuickCommandItem> QuickCommands { get; } =
    [
        new(AIBlockKind.Continue, "续写", "\uE72A"),
        new(AIBlockKind.Rewrite, "重写", "\uE70F"),
        new(AIBlockKind.Expand, "扩写", "\uE740"),
        new(AIBlockKind.Abbreviate, "缩写", "\uE73F"),
        new(AIBlockKind.ChangeDialogue, "改对白", "\uE8BD"),
        new(AIBlockKind.ChangeDescription, "改描写", "\uE790"),
        new(AIBlockKind.AddDetail, "加细节", "\uE710"),
        new(AIBlockKind.AddConflict, "加冲突", "\uE945"),
        new(AIBlockKind.AddPsychology, "加心理", "\uE9A9"),
        new(AIBlockKind.AddForeshadow, "加伏笔", "\uE7C1"),
        new(AIBlockKind.AdjustPace, "调节奏", "\uE916"),
        new(AIBlockKind.ToFirstPerson, "转第一人称", "\uE77B"),
        new(AIBlockKind.ToThirdPerson, "转第三人称", "\uE716"),
    ];

    // ================= 命令 =================

    [RelayCommand]
    public void SelectChapter(Chapter chapter)
    {
        SaveCurrentToModel();
        CurrentChapter = chapter;
        EditorContent = chapter.Content;
        Project.CurrentChapterId = chapter.Id;

        UseChapterOverride = chapter.HasParticipationOverride;
        if (chapter.HasParticipationOverride) ChapterParticipation = chapter.ParticipationOverride;

        IsDiffMode = false;
        PendingBlocks.Clear();
        NotifyParticipation();
        Log($"切换到「{chapter.Title}」（AI 生成占比 {chapter.AiRatio}%）");
    }

    /// <summary>从光标处 AI 续写（生成块，不直接写入）。由代码隐藏直接调用。</summary>
    public async Task ContinueAtCursorAsync(string? instruction = null)
    {
        if (CurrentChapter == null) return;
        await RunGenerateAsync(AIBlockKind.Continue, "", CursorPosition, instruction);
    }

    /// <summary>选中文本后执行快捷命令；instruction 为用户可选的修改意见。由代码隐藏直接调用。</summary>
    public async Task RunQuickCommandAsync(AIBlockKind kind, string? instruction = null)
    {
        if (CurrentChapter == null) return;

        // 续写不需要选区；其余命令若无选区则退化为"从光标续写"
        if (kind != AIBlockKind.Continue && string.IsNullOrEmpty(SelectedText))
        {
            StatusText = "请先在正文中选择一段文字";
            Log("提示：该命令需要先选择文本");
            return;
        }

        if (kind == AIBlockKind.Continue || string.IsNullOrEmpty(SelectedText))
        {
            await RunGenerateAsync(kind, "", CursorPosition, instruction);
        }
        else
        {
            // 针对选区的改写 → 进入 Diff 审阅
            await RunDiffAsync(kind, SelectedText, instruction);
        }
    }

    private async Task RunGenerateAsync(AIBlockKind kind, string source, int insertPos, string? instruction)
    {
        IsAiBusy = true;
        StatusText = $"AI 正在{KindLabel(kind)}…";
        try
        {
            var block = await _ai.GenerateBlockAsync(kind, source, Project, EffectiveParticipation, instruction);
            block.InsertPosition = insertPos;
            PendingBlocks.Insert(0, block);
            RightTabIndex = 0; // 切到"生成"标签
            StatusText = $"已生成 1 个{KindLabel(kind)}块，请审阅";
            Log($"AI {KindLabel(kind)}：生成 {block.GeneratedText.Length} 字（参与度 {EffectiveParticipation:0}%）");
        }
        finally { IsAiBusy = false; }
    }

    private async Task RunDiffAsync(AIBlockKind kind, string source, string? instruction)
    {
        IsAiBusy = true;
        StatusText = $"AI 正在{KindLabel(kind)}…";
        try
        {
            var block = await _ai.GenerateBlockAsync(kind, source, Project, EffectiveParticipation, instruction);
            _diffProposed = block.GeneratedText;
            _diffKind = kind;
            DiffTitle = $"{KindLabel(kind)} · Diff 审阅";
            DiffExplanation = block.Explanation;
            BuildDiffLines(source, block.GeneratedText);
            IsDiffMode = true;
            Log($"进入 Diff 审阅：{KindLabel(kind)}");
        }
        finally { IsAiBusy = false; }
    }

    private void BuildDiffLines(string original, string proposed)
    {
        DiffLines.Clear();
        var result = _diff.ComputeDiff(original, proposed, CurrentChapter?.Id ?? "", "quick");
        foreach (var b in result.Blocks)
        {
            switch (b.Type)
            {
                case DiffType.Equal:
                    AddDiffLine(" ", b.OriginalText, DiffLineKind.Equal);
                    break;
                case DiffType.Delete:
                    AddDiffLine("−", b.OriginalText, DiffLineKind.Delete);
                    break;
                case DiffType.Insert:
                    AddDiffLine("＋", b.ProposedText, DiffLineKind.Insert);
                    break;
                default:
                    AddDiffLine("−", b.OriginalText, DiffLineKind.Delete);
                    AddDiffLine("＋", b.ProposedText, DiffLineKind.Insert);
                    break;
            }
        }
    }

    private void AddDiffLine(string prefix, string text, DiffLineKind kind)
    {
        foreach (var line in text.Split('\n'))
            DiffLines.Add(new DiffLineVm(prefix, line, kind));
    }

    [RelayCommand]
    public void AcceptDiff()
    {
        if (CurrentChapter == null || !IsDiffMode) return;
        var before = EditorContent;
        // 用改写后的文本替换选区
        if (SelectionLength > 0 && SelectionStart + SelectionLength <= EditorContent.Length)
            EditorContent = EditorContent.Remove(SelectionStart, SelectionLength)
                                         .Insert(SelectionStart, _diffProposed);
        else
            EditorContent += "\n" + _diffProposed;

        RecordHistory($"{KindLabel(_diffKind)} · {Truncate(_diffProposed, 10)}", KindLabel(_diffKind), before, _diffProposed.Length);
        FinishEdit(_diffProposed.Length, isAi: true);
        IsDiffMode = false;
        StatusText = "已采纳修改";
        Log("Diff：已采纳");
    }

    [RelayCommand]
    public void RejectDiff()
    {
        IsDiffMode = false;
        StatusText = "已拒绝修改";
        Log("Diff：已拒绝");
    }

    [RelayCommand]
    public async Task RegenerateDiffAsync()
    {
        await RunDiffAsync(_diffKind, SelectedText, null);
    }

    /// <summary>把生成块插入正文</summary>
    [RelayCommand]
    public void InsertBlock(AIBlock block)
    {
        if (CurrentChapter == null) return;
        var before = EditorContent;
        int pos = Math.Clamp(block.InsertPosition, 0, EditorContent.Length);
        var insert = (pos > 0 && EditorContent.Length > 0 ? "\n\n" : "") + block.GeneratedText;
        EditorContent = EditorContent.Insert(pos, insert);

        block.Status = AIBlockStatus.Inserted;
        RecordHistory($"{block.KindLabel} · {Truncate(block.GeneratedText, 10)}", block.KindLabel, before, block.GeneratedText.Length);
        FinishEdit(block.GeneratedText.Length, isAi: true);
        PendingBlocks.Remove(block);
        StatusText = "已插入正文";
        Log($"插入生成块：{block.GeneratedText.Length} 字");
    }

    [RelayCommand]
    public async Task RegenerateBlockAsync(AIBlock block)
    {
        IsAiBusy = true;
        StatusText = "AI 正在重新生成…";
        try
        {
            var fresh = await _ai.GenerateBlockAsync(block.Kind, block.SourceText, Project, EffectiveParticipation);
            block.GeneratedText = fresh.GeneratedText;
            block.Explanation = fresh.Explanation;
            block.CreatedAt = DateTime.Now;
            StatusText = "已重新生成";
            Log("生成块：重新生成");
        }
        finally { IsAiBusy = false; }
    }

    [RelayCommand]
    public async Task ContinueBlockAsync(AIBlock block)
    {
        IsAiBusy = true;
        StatusText = "AI 正在继续生成…";
        try
        {
            var more = await _ai.GenerateBlockAsync(AIBlockKind.Continue, block.GeneratedText, Project, EffectiveParticipation);
            block.GeneratedText += "\n\n" + more.GeneratedText;
            StatusText = "已续接内容";
            Log("生成块：继续生成");
        }
        finally { IsAiBusy = false; }
    }

    [RelayCommand]
    public void DiscardBlock(AIBlock block)
    {
        block.Status = AIBlockStatus.Discarded;
        PendingBlocks.Remove(block);
        Log("生成块：已丢弃");
    }

    [RelayCommand]
    public void AddChapter()
    {
        var ch = new Chapter { Title = $"第{Chapters.Count + 1}章", Order = Chapters.Count + 1 };
        Chapters.Add(ch);
        SelectChapter(ch);
        Log("新增章节");
    }

    [RelayCommand]
    public void DeleteChapter(Chapter? chapter)
    {
        if (chapter == null) return;
        int idx = Chapters.IndexOf(chapter);
        bool wasCurrent = chapter == CurrentChapter;
        Chapters.Remove(chapter);
        // 重新排定序号
        for (int i = 0; i < Chapters.Count; i++) Chapters[i].Order = i + 1;
        if (wasCurrent)
        {
            var next = Chapters.ElementAtOrDefault(Math.Max(0, idx - 1));
            if (next != null) SelectChapter(next);
            else { CurrentChapter = null; EditorContent = ""; }
        }
        Log($"已删除章节「{chapter.Title}」");
    }

    [RelayCommand]
    public void Save()
    {
        SaveCurrentToModel();
        Project.LastModifiedAt = DateTime.Now;
        Project.TotalWords = Chapters.Sum(c => c.WordCount);
        StatusText = $"已保存（全书 {Project.TotalWords} 字）";
        Log("已保存");
        AppState.HasUnsavedChanges = false;
        AppState.AutoSaveService.ResetAfterSave();
    }

    [RelayCommand]
    public void RestoreHistory(AIHistoryRecord record)
    {
        if (string.IsNullOrEmpty(record.BeforeSnapshot)) { StatusText = "该记录无可恢复快照（示例数据）"; return; }
        EditorContent = record.BeforeSnapshot;
        foreach (var h in History) h.IsCurrent = false;
        record.IsCurrent = true;
        StatusText = "已恢复到该历史节点";
        Log($"恢复历史：{record.Title}");
    }

    [RelayCommand]
    public void GoOutline() => AppState.Navigate(typeof(Views.OutlinePage));

    [RelayCommand]
    public void GoHome()
    {
        SaveCurrentToModel();
        AppState.Navigate(typeof(Views.HomePage));
    }

    // ================= 联网搜索 =================

    [RelayCommand]
    public async Task SearchWebAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        SearchStatus = $"正在搜索「{SearchQuery}」…";
        SearchResults.Clear();
        SelectedPageContent = null;

        try
        {
            var results = await _webSearch.SearchAsync(SearchQuery, 10);
            foreach (var r in results) SearchResults.Add(r);
            SearchStatus = results.Count > 0
                ? $"找到 {results.Count} 条结果"
                : "未找到结果，请尝试其他关键词";
        }
        catch (Exception ex)
        {
            SearchStatus = $"搜索失败：{ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    public async Task FetchSearchResultAsync(WebSearchResult? result)
    {
        if (result == null) return;

        IsSearching = true;
        SearchStatus = $"正在获取「{result.Title}」内容…";

        try
        {
            var content = await _webSearch.FetchPageAsync(result.Url);
            if (content != null)
            {
                SelectedPageContent = content;
                SearchStatus = "已获取页面内容";
            }
            else
            {
                SearchStatus = "无法获取页面内容";
            }
        }
        catch (Exception ex)
        {
            SearchStatus = $"获取失败：{ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    // ================= 智能设定提取 =================

    [RelayCommand]
    public void OpenExtractionDialog()
    {
        // 由代码隐藏处理 ContentDialog 显示
        // 这个命令触发 View 中的提取对话框
        OnExtractionRequested?.Invoke();
    }

    /// <summary>事件：请求打开文本提取对话框（由 View 处理）</summary>
    public event Action? OnExtractionRequested;

    /// <summary>事件：请求打开人物表格（由 View 处理）</summary>
    public event Action? OnCharactersTableRequested;

    // ================= 人物表格 =================

    [RelayCommand]
    public void OpenCharactersTable()
    {
        OnCharactersTableRequested?.Invoke();
    }

    // ================= 辅助 =================

    partial void OnEditorContentChanged(string value)
    {
        // 边写边同步模型，使撤销/重做与字数统计实时生效
        if (CurrentChapter != null)
        {
            CurrentChapter.Content = value;
            CurrentChapter.WordCount = value.Length;
        }
        AppState.HasUnsavedChanges = true;
        AppState.AutoSaveService.MarkChanged();
    }

    private void SaveCurrentToModel()
    {
        if (CurrentChapter == null) return;
        CurrentChapter.Content = EditorContent;
        CurrentChapter.WordCount = EditorContent.Length;
        CurrentChapter.LastModifiedAt = DateTime.Now;
        if (UseChapterOverride) CurrentChapter.ParticipationOverride = ChapterParticipation;
        else CurrentChapter.ParticipationOverride = -1;
        AppState.HasUnsavedChanges = true;
        AppState.AutoSaveService.MarkChanged();
    }

    private void FinishEdit(int aiWords, bool isAi)
    {
        if (CurrentChapter == null) return;
        CurrentChapter.Content = EditorContent;
        CurrentChapter.WordCount = EditorContent.Length;
        if (isAi) CurrentChapter.AiGeneratedWords += aiWords;
        CurrentChapter.LastModifiedAt = DateTime.Now;
        if (CurrentChapter.Status == ChapterStatus.Draft) CurrentChapter.Status = ChapterStatus.Writing;
    }

    private void RecordHistory(string title, string action, string before, int words)
    {
        History.Insert(0, new AIHistoryRecord
        {
            ChapterId = CurrentChapter?.Id ?? "",
            Title = title,
            ActionLabel = action,
            BeforeSnapshot = before,
            AfterSnapshot = EditorContent,
            AffectedWords = words,
            IsCurrent = true
        });
        foreach (var h in History.Skip(1)) h.IsCurrent = false;
    }

    private void NotifyParticipation()
    {
        OnPropertyChanged(nameof(EffectiveParticipation));
        OnPropertyChanged(nameof(EffectiveForColor));
        OnPropertyChanged(nameof(ParticipationMode));
        OnPropertyChanged(nameof(ParticipationDesc));
        OnPropertyChanged(nameof(CanAutoContinue));
        OnPropertyChanged(nameof(CanAutoExpand));
        OnPropertyChanged(nameof(CanAutoBuildSetting));
        OnPropertyChanged(nameof(CanAdvancePlot));
        OnPropertyChanged(nameof(CanCompleteChapter));
    }

    partial void OnGlobalParticipationChanged(double value)
    {
        Project.Participation.Level = value;
        Project.Participation.NotifyDerived();
        NotifyParticipation();
    }

    partial void OnChapterParticipationChanged(double value) => NotifyParticipation();

    partial void OnUseChapterOverrideChanged(bool value)
    {
        if (value && CurrentChapter != null) ChapterParticipation = GlobalParticipation;
        NotifyParticipation();
    }

    partial void OnSelectedTextChanged(string value) => OnPropertyChanged(nameof(HasSelection));

    private void Log(string msg)
    {
        LogText = $"[{DateTime.Now:HH:mm:ss}] {msg}\n{LogText}";
        var lines = LogText.Split('\n');
        if (lines.Length > 60) LogText = string.Join('\n', lines.Take(60));
    }

    private static string Truncate(string s, int n)
    {
        s = s.Replace("\n", "").Trim();
        return s.Length <= n ? s : s[..n] + "…";
    }

    private static string ModeOf(double lvl) => lvl switch
    {
        <= 5 => "纯人工模式",
        <= 35 => "轻度辅助",
        <= 65 => "协同创作",
        <= 90 => "AI 主写",
        _ => "全自动创作"
    };

    private static string DescOf(double lvl) => lvl switch
    {
        <= 5 => "AI 仅提供润色与设定冲突提醒，不主动生成正文。",
        <= 35 => "AI 可续写短句、给出剧情方向，正文以你为主。",
        <= 65 => "你与 AI 一人一句共同创作，AI 主动补充描写与对白。",
        <= 90 => "你给出目标与概要，AI 自动写完整正文，你负责修改把控。",
        _ => "你只需提供世界观与大纲，AI 自动完成分章与正文。"
    };

    private string KindLabel(AIBlockKind kind) => new AIBlock { Kind = kind }.KindLabel;
}

/// <summary>快捷命令描述</summary>
public record QuickCommandItem(AIBlockKind Kind, string Label, string Glyph);

/// <summary>Diff 展示行</summary>
public enum DiffLineKind { Equal, Insert, Delete }

public partial class DiffLineVm : ObservableObject
{
    public DiffLineVm(string prefix, string text, DiffLineKind kind)
    {
        Prefix = prefix;
        Text = text;
        Kind = kind;
    }

    public string Prefix { get; }
    public string Text { get; }
    public DiffLineKind Kind { get; }

    public Brush Background => Kind switch
    {
        DiffLineKind.Insert => new SolidColorBrush(ColorHelper.FromArgb(46, 0x10, 0xA3, 0x7A)),
        DiffLineKind.Delete => new SolidColorBrush(ColorHelper.FromArgb(40, 0xE0, 0x2E, 0x3B)),
        _ => new SolidColorBrush(Colors.Transparent)
    };

    public Brush Foreground => Kind switch
    {
        DiffLineKind.Insert => new SolidColorBrush(ColorHelper.FromArgb(255, 0x0B, 0x7A, 0x5A)),
        DiffLineKind.Delete => new SolidColorBrush(ColorHelper.FromArgb(255, 0xB4, 0x22, 0x2E)),
        _ => new SolidColorBrush(ColorHelper.FromArgb(255, 0x3A, 0x3A, 0x3A))
    };
}
