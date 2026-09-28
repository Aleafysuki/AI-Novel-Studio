namespace AINovelWriter.Models;

/// <summary>
/// 网络搜索结果条目
/// </summary>
public class WebSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    /// <summary>搜索结果来源（DuckDuckGo / Bing）</summary>
    public string Source { get; set; } = "DuckDuckGo";
}

/// <summary>
/// 网页抓取结果（用户点击搜索结果后获取的页面内容）
/// </summary>
public class WebPageContent
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsTruncated { get; set; }
}
