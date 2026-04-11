using System.Net;

namespace TextGrab.Shared;

/// <summary>
/// A single post-grab action (identified by Key, rendered with Label+Glyph).
/// </summary>
public record PostGrabAction(string ActionId, string Label, string Glyph);

/// <summary>
/// Available post-grab actions and their execution.
/// Port of WPF PostGrabActionManager, simplified for Uno.
/// </summary>
public static class PostGrabActionManager
{
    public const string FixGuids = "FixGuids";
    public const string TrimEachLine = "TrimEachLine";
    public const string RemoveDuplicateLines = "RemoveDuplicateLines";
    public const string WebSearch = "WebSearch";

    /// <summary>
    /// All available actions in display order.
    /// </summary>
    public static readonly IReadOnlyList<PostGrabAction> AllActions =
    [
        new(FixGuids,            "Fix GUIDs",             "\uE8EF"), // Braces/format
        new(TrimEachLine,        "Trim each line",        "\uE9A9"), // Collapse
        new(RemoveDuplicateLines,"Remove duplicate lines","\uE74D"), // Delete
        new(WebSearch,           "Web Search",            "\uE721"), // Search
    ];

    /// <summary>
    /// Parses the comma-separated enabled-keys string into a HashSet.
    /// </summary>
    public static HashSet<string> ParseEnabled(string enabledCsv)
    {
        if (string.IsNullOrWhiteSpace(enabledCsv))
            return [];
        return [.. enabledCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    /// <summary>
    /// Serializes a set of enabled keys back to a CSV string.
    /// </summary>
    public static string SerializeEnabled(IEnumerable<string> enabledKeys)
        => string.Join(",", enabledKeys);

    /// <summary>
    /// Applies all enabled post-grab actions to the captured text in order.
    /// Returns the possibly-transformed text (Web Search doesn't modify text, just launches).
    /// </summary>
    public static async Task<string> ApplyEnabledActionsAsync(
        string text,
        HashSet<string> enabledKeys,
        string webSearchUrl = "https://www.google.com/search?q=")
    {
        if (enabledKeys.Count == 0 || string.IsNullOrEmpty(text))
            return text;

        string result = text;

        foreach (var action in AllActions)
        {
            if (!enabledKeys.Contains(action.ActionId))
                continue;

            result = action.ActionId switch
            {
                FixGuids => result.CorrectCommonGuidErrors(),
                TrimEachLine => TrimEachLineImpl(result),
                RemoveDuplicateLines => result.RemoveDuplicateLines(),
                WebSearch => await WebSearchAsync(result, webSearchUrl).ConfigureAwait(true),
                _ => result,
            };
        }

        return result;
    }

    private static string TrimEachLineImpl(string text)
    {
        var lines = text
            .Split(Environment.NewLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToArray();

        return lines.Length == 0
            ? string.Empty
            : string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static async Task<string> WebSearchAsync(string text, string webSearchUrl)
    {
        try
        {
            string encoded = WebUtility.UrlEncode(text);
            var uri = new Uri($"{webSearchUrl}{encoded}");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        catch
        {
            // Swallow launch errors — user can retry from clipboard
        }
        return text; // Web search doesn't mutate the text
    }
}
