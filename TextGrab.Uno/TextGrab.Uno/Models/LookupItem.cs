using System;
using System.Linq;

namespace TextGrab.Models;

public enum LookupItemKind
{
    Simple = 0,
    EditWindow = 1,
    GrabFrame = 2,
    Link = 3,
    Command = 4,
    Dynamic = 5,
}

public class LookupItem : IEquatable<LookupItem>
{
    public string ShortValue { get; set; } = string.Empty;
    public string LongValue { get; set; } = string.Empty;
    public LookupItemKind Kind { get; set; } = LookupItemKind.Simple;
    public HistoryInfo? HistoryItem { get; set; }

    /// <summary>
    /// Segoe MDL2 Assets glyph for this item's Kind.
    /// </summary>
    public string IconGlyph => Kind switch
    {
        LookupItemKind.Simple => "\uE8C8",       // Copy
        LookupItemKind.EditWindow => "\uE8A7",   // OpenWith (window)
        LookupItemKind.GrabFrame => "\uE71D",    // Picture
        LookupItemKind.Link => "\uE71B",         // Link
        LookupItemKind.Command => "\uE756",      // CommandPrompt
        LookupItemKind.Dynamic => "\uE945",      // Lightning
        _ => "\uE8C8",
    };

    public string FirstLettersString
        => string.Join("", ShortValue.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.Length > 0).Select(s => s[0])).ToLower();

    public LookupItem() { }

    public LookupItem(string sv, string lv)
    {
        ShortValue = sv;
        LongValue = lv;
    }

    public LookupItem(HistoryInfo historyInfo)
    {
        ShortValue = historyInfo.CaptureDateTime.ToString("F");
        LongValue = historyInfo.TextContent.Length > 100
            ? historyInfo.TextContent[..100].Trim() + "..."
            : historyInfo.TextContent.Trim();

        HistoryItem = historyInfo;
        Kind = string.IsNullOrEmpty(historyInfo.ImagePath)
            ? LookupItemKind.EditWindow
            : LookupItemKind.GrabFrame;
    }

    public override string ToString()
    {
        if (HistoryItem is not null)
            return $"{HistoryItem.CaptureDateTime:F} {HistoryItem.TextContent}";

        return $"{ShortValue} {LongValue}";
    }

    public string ToCSVString() => $"{ShortValue},{LongValue}";

    public static LookupItem ParseCSVLine(string line)
    {
        int commaIndex = line.IndexOf(',');
        if (commaIndex < 0)
            return new LookupItem(line.Trim(), string.Empty);

        return new LookupItem(
            line[..commaIndex].Trim(),
            line[(commaIndex + 1)..].Trim());
    }

    public static LookupItem ParseTabLine(string line)
    {
        int tabIndex = line.IndexOf('\t');
        if (tabIndex < 0)
            return new LookupItem(line.Trim(), string.Empty);

        string shortVal = line[..tabIndex].Trim();
        string longVal = line[(tabIndex + 1)..].Trim();

        // Detect special kinds from content
        LookupItemKind kind = LookupItemKind.Simple;
        if (longVal.StartsWith('>'))
        {
            kind = LookupItemKind.Command;
            longVal = longVal[1..].Trim();
        }
        else if (longVal.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            kind = LookupItemKind.Link;
        }

        return new LookupItem(shortVal, longVal) { Kind = kind };
    }

    public bool Equals(LookupItem? other)
    {
        if (other is null) return false;
        return other.ToString() == ToString();
    }
}
