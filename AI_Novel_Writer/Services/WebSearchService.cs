using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using AINovelWriter.Models;

namespace AINovelWriter.Services;

/// <summary>
/// 本地联网搜索服务 —— 直接抓取搜索引擎 HTML 结果页，无需第三方 API 密钥。
/// 使用 DuckDuckGo（不要求 API Key）作为默认搜索引擎；可选退回到 Bing HTML。
/// </summary>
public partial class WebSearchService
{
    private readonly HttpClient _http;
    private const string DuckDuckGoUrl = "https://html.duckduckgo.com/html/?q=";
    private const string BingUrl = "https://www.bing.com/search?q=";

    public WebSearchService()
    {
        _http = new HttpClient(new HttpClientHandler
        {
            // 模拟标准浏览器 User-Agent，避免被搜引起引擎识别为机器人而屏蔽
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        });

        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9,en;q=0.8");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// 搜索互联网，返回结果列表
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="maxResults">最大返回数（默认 10）</param>
    public async Task<List<WebSearchResult>> SearchAsync(string query, int maxResults = 10)
    {
        var results = new List<WebSearchResult>();

        try
        {
            var url = DuckDuckGoUrl + HttpUtility.UrlEncode(query);
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            results = ParseDuckDuckGoResults(html, maxResults);

            // 如果 DuckDuckGo 无结果，退回到 Bing
            if (results.Count == 0)
            {
                results = await SearchBingAsync(query, maxResults);
            }
        }
        catch (Exception ex)
        {
            // 记录日志，返回空结果
            System.Diagnostics.Debug.WriteLine($"[WebSearch] DuckDuckGo failed: {ex.Message}");
            try
            {
                results = await SearchBingAsync(query, maxResults);
            }
            catch
            {
                // 两个搜索引擎都失败
            }
        }

        return results;
    }

    /// <summary>
    /// 抓取指定 URL 的页面内容文本
    /// </summary>
    public async Task<WebPageContent?> FetchPageAsync(string url)
    {
        try
        {
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            // 提取页面标题
            var titleMatch = Regex.Match(html, @"<title>\s*(.*?)\s*</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var title = titleMatch.Success
                ? HttpUtility.HtmlDecode(titleMatch.Groups[1].Value)
                : "无标题";

            // 去除 HTML 标签，提取纯文本
            var text = StripHtmlTags(html);
            // 合并空白并限制长度（最多 5000 字符）
            text = Regex.Replace(text, @"\s+", " ");
            text = text.Trim();
            var isTruncated = text.Length > 5000;
            if (isTruncated)
                text = text[..5000] + "...";

            return new WebPageContent
            {
                Title = title,
                Url = url,
                Content = text,
                IsTruncated = isTruncated
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebSearch] FetchPage failed: {ex.Message}");
            return null;
        }
    }

    #region DuckDuckGo 解析

    private static List<WebSearchResult> ParseDuckDuckGoResults(string html, int maxResults)
    {
        var results = new List<WebSearchResult>();

        // DuckDuckGo HTML 结果格式：每条结果用 <a rel="nofollow" class="result__a" href="...">
        // 和 <a class="result__snippet" ...> 结构表示
        var resultBlocks = Regex.Split(html, @"<article\s+class=""[^""]*result[^""]*"">")
            .Skip(1) // 第一个 split 前的内容忽略
            .ToArray();

        foreach (var block in resultBlocks)
        {
            if (results.Count >= maxResults) break;

            // 提取标题 + URL
            var linkMatch = Regex.Match(block,
                @"<a\s+rel=""[^""]*""\s+class=""[^""]*result__a[^""]*""\s+href=""(?<url>[^""]+)""[^>]*>(?<title>.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!linkMatch.Success) continue;

            var title = HttpUtility.HtmlDecode(Regex.Replace(linkMatch.Groups["title"].Value, @"<[^>]+>", ""));
            var url = linkMatch.Groups["url"].Value;

            // 提取摘要
            var snippetMatch = Regex.Match(block,
                @"<a\s+class=""[^""]*result__snippet[^""]*""[^>]*>(?<snippet>.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var snippet = snippetMatch.Success
                ? HttpUtility.HtmlDecode(Regex.Replace(snippetMatch.Groups["snippet"].Value, @"<[^>]+>", ""))
                : "";

            // 清理多余空格
            title = Regex.Replace(title.Trim(), @"\s+", " ");
            snippet = Regex.Replace(snippet.Trim(), @"\s+", " ");

            if (string.IsNullOrWhiteSpace(title)) continue;

            results.Add(new WebSearchResult
            {
                Title = title,
                Url = url,
                Snippet = snippet,
                Source = "DuckDuckGo"
            });
        }

        // 如果 article 结构没匹配到，退回到旧的解析方式
        if (results.Count == 0)
        {
            results = ParseDuckDuckGoLegacy(html, maxResults);
        }

        return results;
    }

    private static List<WebSearchResult> ParseDuckDuckGoLegacy(string html, int maxResults)
    {
        var results = new List<WebSearchResult>();
        var resultRegex = ResultRegex();
        var matches = resultRegex.Matches(html);

        foreach (Match m in matches)
        {
            if (results.Count >= maxResults) break;
            var title = HttpUtility.HtmlDecode(Regex.Replace(m.Groups["title"].Value, @"<[^>]+>", "")).Trim();
            var url = m.Groups["url"].Value;
            var snippet = HttpUtility.HtmlDecode(Regex.Replace(m.Groups["snippet"].Value, @"<[^>]+>", "")).Trim();

            if (string.IsNullOrWhiteSpace(title)) continue;

            results.Add(new WebSearchResult
            {
                Title = Regex.Replace(title, @"\s+", " "),
                Url = url,
                Snippet = Regex.Replace(snippet, @"\s+", " "),
                Source = "DuckDuckGo"
            });
        }

        return results;
    }

    [GeneratedRegex(@"<a\s+rel=""[^""]*""\s+class=""[^""]*result__a[^""]*""\s+href=""(?<url>[^""]+)""[^>]*>\s*(?<title>.*?)\s*</a>.*?<a\s+class=""[^""]*result__snippet[^""]*""[^>]*>(?<snippet>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex ResultRegex();

    #endregion

    #region Bing 退回到解析

    private async Task<List<WebSearchResult>> SearchBingAsync(string query, int maxResults)
    {
        var results = new List<WebSearchResult>();
        var url = BingUrl + HttpUtility.UrlEncode(query);
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Bing HTML 结果用 <li class="b_algo"> 包裹每条结果
        var blocks = Regex.Split(html, @"<li\s+class=""[^""]*b_algo[^""]*""")
            .Skip(1)
            .ToArray();

        foreach (var block in blocks)
        {
            if (results.Count >= maxResults) break;

            var linkMatch = Regex.Match(block,
                @"<a\s+href=""(?<url>https?://[^""]+)""[^>]*>(?<title>.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!linkMatch.Success) continue;

            var title = HttpUtility.HtmlDecode(Regex.Replace(linkMatch.Groups["title"].Value, @"<[^>]+>", ""));
            var urlVal = linkMatch.Groups["url"].Value;

            var snippetMatch = Regex.Match(block,
                @"<p[^>]*>(?<snippet>.*?)</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var snippet = snippetMatch.Success
                ? HttpUtility.HtmlDecode(Regex.Replace(snippetMatch.Groups["snippet"].Value, @"<[^>]+>", ""))
                : "";

            if (string.IsNullOrWhiteSpace(title)) continue;

            results.Add(new WebSearchResult
            {
                Title = Regex.Replace(title.Trim(), @"\s+", " "),
                Url = urlVal,
                Snippet = Regex.Replace(snippet.Trim(), @"\s+", " "),
                Source = "Bing"
            });
        }

        return results;
    }

    #endregion

    #region HTML 清理

    /// <summary>
    /// 去除 HTML 标签，保留纯文本
    /// </summary>
    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // 去除 script 和 style 块
        html = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 替换常见块级标签为换行
        html = Regex.Replace(html, @"</?(?:p|div|h[1-6]|li|br|tr|blockquote|pre)[^>]*>", "\n", RegexOptions.IgnoreCase);

        // 去除剩余 HTML 标签
        html = Regex.Replace(html, @"<[^>]+>", " ");

        // HTML 实体解码
        html = HttpUtility.HtmlDecode(html);

        return html;
    }

    #endregion

    public void Dispose()
    {
        _http?.Dispose();
    }
}
