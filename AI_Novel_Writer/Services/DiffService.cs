using AINovelWriter.Models;
using System.Text;

namespace AINovelWriter.Services;

/// <summary>
/// 文本 Diff 服务：使用简单的 LCS 算法计算文本差异
/// 原型阶段用实现 Myer's Diff 的简化版
/// </summary>
public class DiffService
{
    /// <summary>
    /// 计算两段文本的差异
    /// </summary>
    public DiffResult ComputeDiff(string original, string proposed, string chapterId, string operationType)
    {
        var originalLines = original.Split('\n');
        var proposedLines = proposed.Split('\n');

        var diffBlocks = ComputeLineDiff(originalLines, proposedLines);

        return new DiffResult
        {
            ChapterId = chapterId,
            Blocks = diffBlocks,
            OperationType = operationType,
            GeneratedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 简单的逐行 Diff（基于 LCS）
    /// </summary>
    private List<DiffBlock> ComputeLineDiff(string[] original, string[] proposed)
    {
        var blocks = new List<DiffBlock>();
        var dmp = new DiffMatchPatchWrapper();
        var diffs = dmp.DiffMain(string.Join("\n", original), string.Join("\n", proposed));
        dmp.DiffCleanupSemantic(diffs);

        int blockId = 0;
        foreach (var diff in diffs)
        {
            var type = diff.Operation switch
            {
                OperationType.Equal => DiffType.Equal,
                OperationType.Delete => DiffType.Delete,
                OperationType.Insert => DiffType.Insert,
                _ => DiffType.Replace
            };

            blocks.Add(new DiffBlock
            {
                Type = type,
                OriginalText = diff.Operation == OperationType.Insert ? string.Empty : diff.Text,
                ProposedText = diff.Operation == OperationType.Delete ? string.Empty : diff.Text,
                StartLine = blockId,
                EndLine = blockId + 1,
                Reason = type switch
                {
                    DiffType.Insert => "新增内容",
                    DiffType.Delete => "删除内容",
                    DiffType.Replace => "替换内容",
                    _ => "无变化"
                }
            });

            blockId++;
        }

        return blocks;
    }

    /// <summary>
    /// 生成 HTML 格式的 Diff 视图
    /// </summary>
    public string GenerateHtmlDiff(string original, string proposed)
    {
        var dmp = new DiffMatchPatchWrapper();
        var diffs = dmp.DiffMain(original, proposed);
        dmp.DiffCleanupSemantic(diffs);

        var sb = new StringBuilder();
        sb.Append("<div style='font-family: monospace; white-space: pre-wrap;'>");

        foreach (var diff in diffs)
        {
            switch (diff.Operation)
            {
                case OperationType.Equal:
                    sb.Append($"<span>{System.Net.WebUtility.HtmlEncode(diff.Text)}</span>");
                    break;
                case OperationType.Insert:
                    sb.Append($"<span style='background-color: #d4edda; text-decoration: none;'>"
                              + $"{System.Net.WebUtility.HtmlEncode(diff.Text)}</span>");
                    break;
                case OperationType.Delete:
                    sb.Append($"<span style='background-color: #f8d7da; text-decoration: line-through;'>"
                              + $"{System.Net.WebUtility.HtmlEncode(diff.Text)}</span>");
                    break;
            }
        }

        sb.Append("</div>");
        return sb.ToString();
    }
}

/// <summary>
/// 简化的 Diff-Match-Patch 包装器
/// </summary>
public class DiffMatchPatchWrapper
{
    public List<Diff> DiffMain(string text1, string text2)
    {
        var diffs = new List<Diff>();

        if (text1 == text2)
        {
            if (!string.IsNullOrEmpty(text1))
                diffs.Add(new Diff(OperationType.Equal, text1));
            return diffs;
        }

        // 使用简单的行级对比
        var lines1 = text1.Split('\n');
        var lines2 = text2.Split('\n');

        var lcs = LongestCommonSubsequence(lines1, lines2, out var matches);

        int i = 0, j = 0;
        foreach (var (matchI, matchJ) in matches)
        {
            // 删除的行（在 text1 中但不在 text2 中）
            while (i < matchI)
            {
                diffs.Add(new Diff(OperationType.Delete, lines1[i]));
                i++;
            }

            // 插入的行（在 text2 中但不在 text1 中）
            while (j < matchJ)
            {
                diffs.Add(new Diff(OperationType.Insert, lines2[j]));
                j++;
            }

            // 相同的行
            diffs.Add(new Diff(OperationType.Equal, lines1[matchI]));
            i = matchI + 1;
            j = matchJ + 1;
        }

        // 处理剩余的删除行
        while (i < lines1.Length)
        {
            diffs.Add(new Diff(OperationType.Delete, lines1[i]));
            i++;
        }

        // 处理剩余的插入行
        while (j < lines2.Length)
        {
            diffs.Add(new Diff(OperationType.Insert, lines2[j]));
            j++;
        }

        return diffs;
    }

    public void DiffCleanupSemantic(List<Diff> diffs)
    {
        // 合并相邻的同类 diff
        for (int i = diffs.Count - 1; i > 0; i--)
        {
            if (diffs[i].Operation == diffs[i - 1].Operation)
            {
                diffs[i - 1].Text += diffs[i].Text;
                diffs.RemoveAt(i);
            }
        }
    }

    private List<string> LongestCommonSubsequence(string[] a, string[] b,
        out List<(int, int)> matches)
    {
        int m = a.Length, n = b.Length;
        var dp = new int[m + 1, n + 1];

        for (int i = 1; i <= m; i++)
        for (int j = 1; j <= n; j++)
        {
            if (a[i - 1] == b[j - 1])
                dp[i, j] = dp[i - 1, j - 1] + 1;
            else
                dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
        }

        // 回溯找到匹配位置
        matches = [];
        int x = m, y = n;
        while (x > 0 && y > 0)
        {
            if (a[x - 1] == b[y - 1])
            {
                matches.Insert(0, (x - 1, y - 1));
                x--;
                y--;
            }
            else if (dp[x - 1, y] > dp[x, y - 1])
            {
                x--;
            }
            else
            {
                y--;
            }
        }

        return matches.Select(m => a[m.Item1]).ToList();
    }
}

public class Diff
{
    public OperationType Operation { get; set; }
    public string Text { get; set; }

    public Diff(OperationType operation, string text)
    {
        Operation = operation;
        Text = text;
    }
}

public enum OperationType
{
    Equal,
    Delete,
    Insert
}
