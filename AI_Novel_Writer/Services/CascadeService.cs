using AINovelWriter.Models;

namespace AINovelWriter.Services;

/// <summary>
/// 级联修改服务：检测事件修改对后续章节的影响
/// </summary>
public class CascadeService
{
    /// <summary>
    /// 当用户修改一个关键事件后，检测后续依赖冲突
    /// </summary>
    public async Task<List<CascadeConflict>> DetectConflictsAsync(
        NovelEvent modifiedEvent, NovelProject project)
    {
        await Task.Delay(400);

        var conflicts = new List<CascadeConflict>();

        // 查找依赖此事件的后继事件
        var dependentEvents = project.Events
            .Where(e => modifiedEvent.DependentEventIds.Contains(e.Id))
            .ToList();

        foreach (var depEvent in dependentEvents)
        {
            var affectedChapter = project.Chapters
                .FirstOrDefault(c => c.Id == depEvent.ChapterId);

            if (affectedChapter == null) continue;

            var conflict = new CascadeConflict
            {
                SourceEventId = modifiedEvent.Id,
                SourceEventTitle = modifiedEvent.Title,
                SourceChapterId = modifiedEvent.ChapterId,
                AffectedChapterId = affectedChapter.Id,
                AffectedChapterTitle = affectedChapter.Title,
                Severity = DetermineSeverity(modifiedEvent.Nature)
            };

            // 生成冲突描述和修改建议
            if (modifiedEvent.Nature == EventNature.Death)
            {
                conflict.ConflictDescription =
                    $"第{affectedChapter.Order}章「{affectedChapter.Title}」中，"
                    + $"事件「{depEvent.Title}」依赖于「{modifiedEvent.Title}」中角色的死亡状态。"
                    + $"若死亡状态被修改，该章节的相关逻辑将失效。";

                conflict.OriginalText = $"[原文本] {depEvent.Description}（基于死亡状态）";
                conflict.SuggestedText = $"[建议修改] {depEvent.Description}（调整为基于新状态的描述）";
            }
            else
            {
                conflict.ConflictDescription =
                    $"检测到第{affectedChapter.Order}章「{affectedChapter.Title}」与修改的事件"
                    + $"「{modifiedEvent.Title}」存在逻辑依赖。";

                conflict.OriginalText = $"[原文] {depEvent.Description}";
                conflict.SuggestedText = $"[建议] 根据「{modifiedEvent.Title}」的修改，更新相关描述以保持一致性。";
            }

            conflicts.Add(conflict);
        }

        return conflicts;
    }

    private ConflictSeverity DetermineSeverity(EventNature nature) => nature switch
    {
        EventNature.Death => ConflictSeverity.Critical,
        EventNature.Injury or EventNature.Betrayal => ConflictSeverity.High,
        EventNature.Separation or EventNature.Defeat => ConflictSeverity.Medium,
        _ => ConflictSeverity.Low
    };

    /// <summary>
    /// 模拟：用户修改事件后批量检测级联影响
    /// </summary>
    public async Task<List<CascadeConflict>> SimulateCascadeDetectionAsync(NovelProject project)
    {
        // 模拟：假设用户修改了某个关键事件
        if (project.Events.Count == 0) return [];

        var sampleEvent = project.Events.FirstOrDefault(e => e.Nature == EventNature.Death)
                          ?? project.Events[0];

        // 模拟修改（实际项目中这里是用户的实际修改）
        var modifiedEvent = new NovelEvent
        {
            Id = sampleEvent.Id,
            Title = sampleEvent.Title,
            ChapterId = sampleEvent.ChapterId,
            Nature = EventNature.Injury, // 假设从"死亡"改为"受伤"
            DependentEventIds = sampleEvent.DependentEventIds
        };

        return await DetectConflictsAsync(modifiedEvent, project);
    }
}
