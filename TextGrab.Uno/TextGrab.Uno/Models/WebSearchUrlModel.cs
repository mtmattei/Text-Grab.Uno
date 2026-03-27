namespace TextGrab.Models;

public record WebSearchUrlModel
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public override string ToString() => Name;

    public static List<WebSearchUrlModel> GetDefaultWebSearchUrls() =>
    [
        new() { Name = "Google", Url = "https://www.google.com/search?q=" },
        new() { Name = "Bing", Url = "https://www.bing.com/search?q=" },
        new() { Name = "DuckDuckGo", Url = "https://duckduckgo.com/?q=" },
        new() { Name = "Brave", Url = "https://search.brave.com/search?q=" },
        new() { Name = "GitHub Code", Url = "https://github.com/search?type=code&q=" },
        new() { Name = "GitHub Repos", Url = "https://github.com/search?type=repositories&q=" },
    ];
}
