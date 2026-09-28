using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AINovelWriter.Models;
using AINovelWriter.Services;

namespace AINovelWriter.ViewModels;

/// <summary>
/// 大纲规划模式 ViewModel（V1.5）：一句话创意 → AI 大纲 → 逐章确认 → AI 生成正文
/// </summary>
public partial class OutlineViewModel : ObservableObject
{
    private readonly AIService _ai = AppState.AIService;

    public OutlineViewModel()
    {
        Project = AppState.CurrentProject ?? AppState.ProjectService.CreateSampleProject();
        AppState.CurrentProject = Project;
        HasOutline = Project.Outlines.Count > 0;
    }

    public NovelProject Project { get; }
    public ObservableCollection<OutlineNode> Outlines => Project.Outlines;

    [ObservableProperty]
    public partial string IdeaInput { get; set; } = "";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "输入一句话创意，让 AI 为你规划整本书的大纲";

    [ObservableProperty]
    public partial bool HasOutline { get; set; }

    public int ConfirmedCount => Outlines.SelectMany(v => v.Children).Count(c => c.IsConfirmed);
    public int ChapterCount => Outlines.SelectMany(v => v.Children).Count();

    [RelayCommand]
    public async Task GenerateOutlineAsync()
    {
        if (string.IsNullOrWhiteSpace(IdeaInput))
        {
            StatusText = "请先输入一句话创意，例如：一个被逐出师门的少年剑客追寻剑道极致。";
            return;
        }

        IsBusy = true;
        StatusText = "AI 正在规划大纲…";
        try
        {
            var nodes = await _ai.GenerateOutlineAsync(IdeaInput, Project);
            Outlines.Clear();
            foreach (var n in nodes) Outlines.Add(n);
            HasOutline = true;
            NotifyCounts();
            StatusText = $"已生成 {Outlines.Count} 卷、{ChapterCount} 章大纲，请逐章确认后生成正文";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void ToggleConfirm(OutlineNode node)
    {
        node.IsConfirmed = !node.IsConfirmed;
        NotifyCounts();
    }

    [RelayCommand]
    public async Task GenerateBodyAsync(OutlineNode chapter)
    {
        IsBusy = true;
        StatusText = $"AI 正在为「{chapter.Title}」生成正文…";
        try
        {
            var body = await _ai.GenerateBodyFromOutlineAsync(chapter, Project);

            Chapter target;
            if (!string.IsNullOrEmpty(chapter.LinkedChapterId) &&
                Project.Chapters.FirstOrDefault(c => c.Id == chapter.LinkedChapterId) is { } existing)
            {
                target = existing;
            }
            else
            {
                target = new Chapter
                {
                    Title = chapter.Title,
                    Order = Project.Chapters.Count + 1,
                    Status = ChapterStatus.Writing
                };
                Project.Chapters.Add(target);
                chapter.LinkedChapterId = target.Id;
            }

            target.Content = body;
            target.WordCount = body.Length;
            target.AiGeneratedWords = body.Length;
            target.AiSummary = chapter.Summary;
            chapter.HasContent = true;
            chapter.IsConfirmed = true;
            NotifyCounts();
            StatusText = $"「{chapter.Title}」正文已生成（{body.Length} 字）";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void OpenInEditor(OutlineNode chapter)
    {
        if (!string.IsNullOrEmpty(chapter.LinkedChapterId))
            Project.CurrentChapterId = chapter.LinkedChapterId;
        AppState.Navigate(typeof(Views.EditorPage));
    }

    [RelayCommand]
    public void GoEditor() => AppState.Navigate(typeof(Views.EditorPage));

    [RelayCommand]
    public void GoHome() => AppState.Navigate(typeof(Views.HomePage));

    private void NotifyCounts()
    {
        OnPropertyChanged(nameof(ConfirmedCount));
        OnPropertyChanged(nameof(ChapterCount));
    }
}
